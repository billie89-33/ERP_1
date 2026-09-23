using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

namespace JamineERP.Backend.Controllers;

public class StorefrontRegisterDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class StorefrontLoginDto
{
    public string Email { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

[Route("api/[controller]")]
[ApiController]
public class StorefrontAuthController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;
    private readonly IWebHostEnvironment _env;

    public StorefrontAuthController(AppDbContext context, IConfiguration configuration, IWebHostEnvironment env)
    {
        _context = context;
        _configuration = configuration;
        _env = env;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(StorefrontRegisterDto dto)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Check if email already exists in Users
            if (await _context.Users.AnyAsync(u => u.Username == dto.Email))
            {
                return BadRequest(new { message = "Email already registered." });
            }

            // 2. Create User (Role = Customer)
            var user = new User
            {
                Username = dto.Email,
                Email = dto.Email,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(dto.Password),
                Role = "Customer"
            };
            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 3. Create or Update Customer Profile (B2C)
            // Sometimes they checked out as guest before, so a Customer might exist with TaxId = Email
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.TaxId == dto.Email);
            if (customer == null)
            {
                customer = new Customer
                {
                    CompanyName = $"{dto.FirstName} {dto.LastName}",
                    TaxId = dto.Email,
                    Phone = dto.Phone,
                    CustomerType = "B2C",
                    UserId = user.Id
                };
                _context.Customers.Add(customer);
            }
            else
            {
                customer.UserId = user.Id;
                customer.Phone = dto.Phone;
                customer.CustomerType = "B2C";
            }
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            // 4. Auto login after register
            var token = GenerateJwtToken(user);
            SetJwtCookie(token);

            return Ok(new { token, user = new { user.Id, user.Username, user.Role, user.FirstName, user.LastName } });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "Registration failed.", error = ex.Message });
        }
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(StorefrontLoginDto dto)
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.Username == dto.Email && u.Role == "Customer");
        if (user == null || !BCrypt.Net.BCrypt.Verify(dto.Password, user.PasswordHash))
        {
            return Unauthorized(new { message = "Invalid email or password." });
        }

        var token = GenerateJwtToken(user);
        SetJwtCookie(token);

        return Ok(new { token, user = new { user.Id, user.Username, user.Role, user.FirstName, user.LastName } });
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

    private void SetJwtCookie(string token)
    {
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = !_env.IsDevelopment(),
            SameSite = _env.IsDevelopment() ? SameSiteMode.Lax : SameSiteMode.None,
            Expires = DateTime.UtcNow.AddDays(7)
        };
        Response.Cookies.Append("jwt", token, cookieOptions);
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
            new Claim("FirstName", user.FirstName),
            new Claim("LastName", user.LastName),
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
