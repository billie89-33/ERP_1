using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using System.ComponentModel.DataAnnotations;

namespace JamineERP.Backend.Controllers;

[ApiController]
[Route("api/[controller]")]
[Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin,Purchasing")]
public class SuppliersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SuppliersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetSuppliers()
    {
        var suppliers = await _context.Suppliers
            .OrderBy(s => s.CompanyName)
            .ToListAsync();
        return Ok(new { success = true, data = suppliers });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetSupplier(Guid id)
    {
        var supplier = await _context.Suppliers.FindAsync(id);
        if (supplier == null) return NotFound();
        return Ok(new { success = true, data = supplier });
    }

    [HttpPost]
    public async Task<IActionResult> CreateSupplier([FromBody] CreateSupplierDto dto)
    {
        // 1. Data Integrity Check: Check for duplicate CompanyName or TaxId
        var duplicate = await _context.Suppliers.FirstOrDefaultAsync(s => 
            s.CompanyName.ToLower() == dto.CompanyName.ToLower() || 
            (!string.IsNullOrEmpty(dto.TaxId) && s.TaxId == dto.TaxId));

        if (duplicate != null)
        {
            if (duplicate.CompanyName.Equals(dto.CompanyName, StringComparison.OrdinalIgnoreCase))
            {
                return BadRequest(new { success = false, message = "ชื่อบริษัทนี้มีอยู่ในระบบแล้ว (Company Name already exists)" });
            }
            if (!string.IsNullOrEmpty(dto.TaxId) && duplicate.TaxId == dto.TaxId)
            {
                return BadRequest(new { success = false, message = "หมายเลขผู้เสียภาษีนี้มีอยู่ในระบบแล้ว (Tax ID already exists)" });
            }
        }

        var supplier = new Supplier
        {
            CompanyName = dto.CompanyName,
            ContactName = dto.ContactName,
            Email = dto.Email,
            Phone = dto.Phone,
            Address = dto.Address,
            TaxId = dto.TaxId,
            PaymentTerms = dto.PaymentTerms
        };

        _context.Suppliers.Add(supplier);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetSupplier), new { id = supplier.Id }, new { success = true, data = supplier });
    }
}

public class CreateSupplierDto
{
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public string? PaymentTerms { get; set; }
}
