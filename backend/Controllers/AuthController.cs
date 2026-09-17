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

    public AuthController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
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

        // Optional: Set token in a HttpOnly Cookie
        var cookieOptions = new CookieOptions
        {
            HttpOnly = true,
            Secure = true, // Set to false if not using HTTPS locally
            SameSite = SameSiteMode.Strict,
            Expires = DateTime.UtcNow.AddHours(2)
        };
        Response.Cookies.Append("jwt", token, cookieOptions);

        return Ok(new { token, user = new { user.Id, user.Username, user.Role } });
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
            expires: DateTime.UtcNow.AddHours(2),
            signingCredentials: credentials);

        return new JwtSecurityTokenHandler().WriteToken(token);
    }
}
