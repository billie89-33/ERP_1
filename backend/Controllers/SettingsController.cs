using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class SettingsController : ControllerBase
{
    private readonly AppDbContext _context;

    public SettingsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("company")]
    public async Task<IActionResult> GetCompanySettings()
    {
        var settings = await _context.CompanySettings.FirstOrDefaultAsync();
        if (settings == null)
        {
            // Default mock settings if none exist
            settings = new CompanySettings
            {
                CompanyName = "Jamine Enterprise Ltd.",
                TaxId = "0000000000000",
                Address = "123 Business Road, Bangkok 10110",
                Phone = "02-XXX-XXXX",
                Email = "contact@jamine.com",
                LogoUrl = ""
            };
            _context.CompanySettings.Add(settings);
            await _context.SaveChangesAsync();
        }

        return Ok(settings);
    }

    [HttpPut("company")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> UpdateCompanySettings([FromBody] CompanySettings dto)
    {
        var settings = await _context.CompanySettings.FirstOrDefaultAsync();
        
        if (settings == null)
        {
            settings = new CompanySettings();
            _context.CompanySettings.Add(settings);
        }

        settings.CompanyName = dto.CompanyName;
        settings.TaxId = dto.TaxId;
        settings.Address = dto.Address;
        settings.Phone = dto.Phone;
        settings.Email = dto.Email;
        settings.LogoUrl = dto.LogoUrl;
        settings.UpdatedAt = System.DateTime.UtcNow;

        await _context.SaveChangesAsync();

        return Ok(new { message = "Company settings updated successfully.", settings });
    }
}
