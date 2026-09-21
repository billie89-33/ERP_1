using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public AuthController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
    {
        _context = context;
        _configuration = configuration;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(RegisterDto dto)
    {
        if (await _context.Users.AnyAsync(u => u.Username == dto.Username))
        {
            return BadRequest(new { message = "Username already exists." });
        }

        var user = new User
        {
            Username = dto.Username,
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
            Role = dto.Role
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        return StatusCode(201, new { message = "User registered successfully." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Username);
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid username or password." });
        }

        var token = GenerateJwtToken(user);

        // JWT Cookie (7 days)
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(), // false on local HTTP, true on prod HTTPS
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("jwt", token, cookieOptions);

        return Ok(new { token, user = new { user.Id, user.Username, user.Role } });
    }

    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete("jwt", new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None
        });
        return Ok(new { message = "Logged out successfully" });
    }

    [Microsoft.AspNetCore.Authorization.Authorize]
    [HttpGet("me")]
    public IActionResult GetCurrentUser()
    {
        // Read from JWT Claims
        var userId = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;
        var username = User.FindFirst(System.Security.Claims.ClaimTypes.Name)?.Value;
        var role = User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value;

        if (userId == null) return Unauthorized();

        return Ok(new { Id = userId, Username = username, Role = role });
    }

    [HttpGet("reset-admin")]
    public async Task<IActionResult> ResetAdmin()
    {
        var admin = await _context.Users.FirstOrDefaultAsync(u => u.Username == "admin");
        if (admin != null)
        {
            admin.PasswordHash = BCrypt.Net.BCrypt.HashPassword("password123");
            await _context.SaveChangesAsync();
            return Ok("Password reset to 'password123'");
        }
        return NotFound("admin not found");
    }

    private string GenerateJwtToken(User user)
    {
        var key = Environment.GetEnvironmentVariable("Jwt__Key") ?? _configuration["Jwt:Key"];
        var issuer = Environment.GetEnvironmentVariable("Jwt__Issuer") ?? _configuration["Jwt:Issuer"];
        var audience = Environment.GetEnvironmentVariable("Jwt__Audience") ?? _configuration["Jwt:Audience"];

        if (string.IsNullOrEmpty(key)) throw new InvalidOperationException("JWT Key is missing");

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: DateTime.UtcNow.AddDays(7),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
