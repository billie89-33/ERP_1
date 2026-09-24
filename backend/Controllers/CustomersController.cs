using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Sales")]
public class CustomersController : ControllerBase
{
    private readonly AppDbContext _context;

    public CustomersController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetCustomers([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "")
    {
        var query = _context.Customers.Where(c => !c.IsDeleted).AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(c => c.CompanyName.ToLower().Contains(search) || c.TaxId.ToLower().Contains(search) || c.Phone.ToLower().Contains(search));
        }

        int totalCount = await query.CountAsync();

        var customers = await query
            .OrderBy(c => c.CompanyName)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(c => new
            {
                c.Id,
                c.CustomerType,
                c.CompanyName,
                c.TaxId,
                c.CreditTermDays,
                c.CreditLimit,
                c.Address,
                c.Phone,
                c.Email,
                c.UserId
            })
            .ToListAsync();
            
        return Ok(new JamineERP.Backend.Models.Pagination.PaginatedResult<object>
        {
            Items = customers.Cast<object>().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }

    [HttpGet("{id}")]
    public async Task<IActionResult> GetCustomer(Guid id)
    {
        var customer = await _context.Customers
            .FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);

        if (customer == null)
            return NotFound(new { message = "Customer not found." });

        return Ok(customer);
    }

    [HttpPost]
    public async Task<IActionResult> CreateCustomer([FromBody] Customer dto)
    {
        if (string.IsNullOrWhiteSpace(dto.CompanyName))
            return BadRequest(new { message = "Company Name (or Name) is required." });

        if (dto.CustomerType == "B2B")
        {
            if (string.IsNullOrWhiteSpace(dto.TaxId) || dto.TaxId.Length != 13)
                return BadRequest(new { message = "B2B customers require a valid 13-digit Tax ID." });
                
            var exists = await _context.Customers.AnyAsync(c => c.TaxId == dto.TaxId && !c.IsDeleted && c.CustomerType == "B2B");
            if (exists)
                return BadRequest(new { message = "A B2B customer with this Tax ID already exists." });
        }
        else if (dto.CustomerType == "B2C")
        {
            dto.CreditLimit = 0;
            dto.CreditTermDays = 0;
        }

        var customer = new Customer
        {
            CustomerType = dto.CustomerType ?? "B2B",
            CompanyName = dto.CompanyName,
            TaxId = dto.TaxId ?? "",
            CreditTermDays = dto.CreditTermDays,
            CreditLimit = dto.CreditLimit,
            Address = dto.Address ?? "",
            Phone = dto.Phone ?? "",
            Email = dto.Email ?? "",
            UserId = dto.UserId
        };

        _context.Customers.Add(customer);
        await _context.SaveChangesAsync();

        return Ok(new { message = "Customer created successfully.", id = customer.Id });
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> UpdateCustomer(Guid id, [FromBody] Customer dto)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (customer == null) return NotFound(new { message = "Customer not found." });

        if (dto.CustomerType == "B2B")
        {
            if (string.IsNullOrWhiteSpace(dto.TaxId) || dto.TaxId.Length != 13)
                return BadRequest(new { message = "B2B customers require a valid 13-digit Tax ID." });

            if (customer.TaxId != dto.TaxId)
            {
                var exists = await _context.Customers.AnyAsync(c => c.TaxId == dto.TaxId && !c.IsDeleted && c.CustomerType == "B2B");
                if (exists) return BadRequest(new { message = "A B2B customer with this Tax ID already exists." });
            }
        }
        else if (dto.CustomerType == "B2C")
        {
            dto.CreditLimit = 0;
            dto.CreditTermDays = 0;
        }

        customer.CustomerType = dto.CustomerType ?? "B2B";
        customer.CompanyName = dto.CompanyName;
        customer.TaxId = dto.TaxId ?? "";
        customer.CreditTermDays = dto.CreditTermDays;
        customer.CreditLimit = dto.CreditLimit;
        customer.Address = dto.Address ?? "";
        customer.Phone = dto.Phone ?? "";
        customer.Email = dto.Email ?? "";
        // Don't arbitrarily change UserId if not needed, but we'll allow it here
        customer.UserId = dto.UserId;

        await _context.SaveChangesAsync();
        return Ok(new { message = "Customer updated successfully." });
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> DeleteCustomer(Guid id)
    {
        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.Id == id && !c.IsDeleted);
        if (customer == null) return NotFound();

        // Soft delete
        customer.IsDeleted = true;
        await _context.SaveChangesAsync();
        return Ok(new { message = "Customer deleted successfully." });
    }
}
