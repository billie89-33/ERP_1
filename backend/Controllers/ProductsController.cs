using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
// [Authorize] // ปิดไว้ก่อนเพื่อให้ทดสอบง่ายใน Swagger (เปิดตอนใช้งานจริง)
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;

    public ProductsController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/Products (พร้อม Pagination)
    [HttpGet]
    public async Task<IActionResult> GetProducts([FromQuery] int page = 1, [FromQuery] int limit = 10, [FromQuery] string search = "")
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) || p.Sku.ToLower().Contains(search.ToLower()));
        }

        var total = await query.CountAsync();
        var totalPages = (int)Math.Ceiling(total / (double)limit);

        var products = await query
            .OrderByDescending(p => p.Id)
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                Cost = p.Cost,
                StockQuantity = p.StockQuantity,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                Specifications = p.Specifications != null ? JsonDocument.Parse(p.Specifications.RootElement.GetRawText()).RootElement : null
            })
            .ToListAsync();

        return Ok(new
        {
            success = true,
            total,
            page,
            totalPages,
            data = products
        });
    }

    // GET: api/Products/5
    [HttpGet("{id}")]
    public async Task<ActionResult<ProductDto>> GetProduct(Guid id)
    {
        var product = await _context.Products.Include(p => p.Category).FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (product == null)
        {
            return NotFound(new { message = "ไม่พบสินค้าที่คุณค้นหา" });
        }

        var dto = new ProductDto
        {
            Id = product.Id,
            Sku = product.Sku,
            Name = product.Name,
            Price = product.Price,
            Cost = product.Cost,
            StockQuantity = product.StockQuantity,
            Specifications = product.Specifications != null ? JsonDocument.Parse(product.Specifications.RootElement.GetRawText()).RootElement : null,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : "N/A"
        };

        return Ok(dto);
    }

    // POST: api/Products
    [HttpPost]
    // [Authorize(Roles = "Admin,Purchasing")] 
    public async Task<ActionResult<ProductDto>> CreateProduct(CreateProductDto dto)
    {
        if (await _context.Products.AnyAsync(p => p.Sku == dto.Sku))
        {
            return BadRequest(new { message = "SKU นี้มีในระบบแล้ว" });
        }

        var product = new Product
        {
            Sku = dto.Sku,
            Name = dto.Name,
            Price = dto.Price,
            Cost = dto.Cost,
            StockQuantity = dto.StockQuantity,
            CategoryId = dto.CategoryId
        };

        if (dto.Specifications.HasValue)
        {
            product.Specifications = JsonDocument.Parse(dto.Specifications.Value.GetRawText());
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new { message = "สร้างสินค้าสำเร็จ!" });
    }

    // PUT: api/Products/5
    [HttpPut("{id}")]
    // [Authorize(Roles = "Admin,Purchasing")]
    public async Task<IActionResult> UpdateProduct(Guid id, UpdateProductDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product == null)
        {
            return NotFound(new { message = "ไม่พบสินค้าที่คุณค้นหา" });
        }

        if (product.Sku != dto.Sku && await _context.Products.AnyAsync(x => x.Sku == dto.Sku))
        {
            return BadRequest(new { message = "SKU นี้มีในระบบแล้ว" });
        }

        product.Sku = dto.Sku;
        product.Name = dto.Name;
        product.Price = dto.Price;
        product.Cost = dto.Cost;
        product.StockQuantity = dto.StockQuantity;
        product.CategoryId = dto.CategoryId;

        if (dto.Specifications.HasValue)
        {
            product.Specifications = JsonDocument.Parse(dto.Specifications.Value.GetRawText());
        }
        else
        {
            product.Specifications = null;
        }

        await _context.SaveChangesAsync();

        return Ok(new { message = "อัปเดตข้อมูลสำเร็จ" });
    }

    // PATCH: api/Products/5/stock
    [HttpPatch("{id}/stock")]
    // [Authorize(Roles = "Admin,Purchasing,Warehouse")]
    public async Task<IActionResult> PatchStock(Guid id, PatchProductStockDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product == null) return NotFound(new { message = "ไม่พบสินค้าที่คุณค้นหา" });

        product.StockQuantity = dto.StockQuantity;
        
        await _context.SaveChangesAsync();
        return Ok(new { message = "อัปเดตสต๊อกสินค้าสำเร็จ" });
    }

    // PATCH: api/Products/5/price
    [HttpPatch("{id}/price")]
    // [Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> PatchPrice(Guid id, PatchProductPriceDto dto)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product == null) return NotFound(new { message = "ไม่พบสินค้าที่คุณค้นหา" });

        product.Price = dto.Price;
        product.Cost = dto.Cost;

        await _context.SaveChangesAsync();
        return Ok(new { message = "อัปเดตราคาต้นทุนและราคาขายสำเร็จ" });
    }

    // DELETE: api/Products/5
    [HttpDelete("{id}")]
    // [Authorize(Roles = "Admin")]
    public async Task<IActionResult> DeleteProduct(Guid id)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product == null)
        {
            return NotFound(new { message = "ไม่พบสินค้าที่คุณค้นหา" });
        }

        // ระบบ Soft Delete (เราแค่เปลี่ยนสถานะ IsDeleted เป็น true)
        product.IsDeleted = true;
        await _context.SaveChangesAsync();

        return Ok(new { message = "ลบสินค้าเรียบร้อยแล้ว (Soft Delete)" });
    }
}
