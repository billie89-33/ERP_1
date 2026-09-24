using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

public class CreateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

public class UpdateUserDto
{
    public string Username { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
    public string? NewPassword { get; set; }
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
}

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class UsersController : ControllerBase
{
    private readonly AppDbContext _context;

    public UsersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "")
    {
        var query = _context.Users.Where(u => !u.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(u => u.Username.ToLower().Contains(search) || u.Email.ToLower().Contains(search));
        }

        int totalCount = await query.CountAsync();

        var users = await query
            .OrderBy(u => u.Username)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Role,
                u.FirstName,
                u.LastName,
                u.Email
            })
            .ToListAsync();
            
        return Ok(new JamineERP.Backend.Models.Pagination.PaginatedResult<object>
        {
            Items = users.Cast<object>().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetUser(Guid id)
    {
        var user = await _context.Users
            .Where(u => u.Id == id && !u.IsDeleted)
            .Select(u => new
            {
                u.Id,
                u.Username,
                u.Role,
                u.FirstName,
                u.LastName,
                u.Email
            })
            .FirstOrDefaultAsync();

        if (user == null)
            return NotFound(new { message = "User not found." });

        return Ok(user);
    }

    [HttpPost]
    public async Task<IActionResult> CreateUser([FromBody] CreateUserDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Username) || string.IsNullOrWhiteSpace(dto.Password) || string.IsNullOrWhiteSpace(dto.Role))
            return BadRequest(new { message = "Username, Password, and Role are required." });

        if (await _context.Users.AnyAsync(u => u.Username == dto.Username && !u.IsDeleted))
            return BadRequest(new { message = "Username already exists." });

        if (!string.IsNullOrWhiteSpace(dto.Email) && await _context.Users.AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
            return BadRequest(new { message = "Email already exists." });

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role,
            FirstName = dto.FirstName ?? "",
            LastName = dto.LastName ?? "",
            Email = dto.Email ?? ""
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return Ok(new { message = "User created successfully.", id = user.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateUser(Guid id, [FromBody] UpdateUserDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound(new { message = "User not found." });

        // Check duplicate username
        if (user.Username != dto.Username)
        {
            if (await _context.Users.AnyAsync(u => u.Username == dto.Username && !u.IsDeleted))
                return BadRequest(new { message = "Username already exists." });
        }

        // Check duplicate email
        if (user.Email != dto.Email && !string.IsNullOrWhiteSpace(dto.Email))
        {
            if (await _context.Users.AnyAsync(u => u.Email == dto.Email && !u.IsDeleted))
                return BadRequest(new { message = "Email already exists." });
        }

        user.Username = dto.Username;
        user.Role = dto.Role;
        user.FirstName = dto.FirstName ?? "";
        user.LastName = dto.LastName ?? "";
        user.Email = dto.Email ?? "";
        
        if (!string.IsNullOrWhiteSpace(dto.NewPassword))
        {
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.NewPassword);
        }

        await _context.SaveChangesAsync();
        return Ok(new { message = "User updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteUser(Guid id)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id && !u.IsDeleted);
        if (user == null) return NotFound();

        // Prevent admin from deleting themselves (basic safeguard)
        var currentUsername = User.Identity?.Name;
        if (user.Username == currentUsername || user.Username == "admin")
            return BadRequest(new { message = "Cannot delete the main admin or yourself." });

        // Soft delete
        user.IsDeleted = true;
        // Optionally scramble username so it can be reused later
        user.Username = $"{user.Username}_deleted_{Guid.NewGuid()}";
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "User deleted successfully." });
    }
}
