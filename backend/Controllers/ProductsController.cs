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
[Authorize] // เปิดใช้งานจริง
public class ProductsController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JamineERP.Backend.Services.IPhotoService _photoService;

    public ProductsController(AppDbContext context, JamineERP.Backend.Services.IPhotoService photoService)
    {
        _context = context;
        _photoService = photoService;
    }

    // GET: api/Products (พร้อมระบบ Pagination และ Filters)
    [HttpGet]
    [AllowAnonymous]
    public async Task<IActionResult> GetProducts(
        [FromQuery] int page = 1, 
        [FromQuery] int limit = 10, 
        [FromQuery] string search = "",
        [FromQuery] string? categoryName = null,
        [FromQuery] decimal? minPrice = null,
        [FromQuery] decimal? maxPrice = null,
        [FromQuery] string[]? brands = null,
        [FromQuery] string? specs = null) // JSON string format: {"Resolution":["FHD"],"Color":["BLACK"]}
    {
        var query = _context.Products
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            query = query.Where(p => p.Name.ToLower().Contains(search.ToLower()) || p.Sku.ToLower().Contains(search.ToLower()));
        }
        
        if (!string.IsNullOrEmpty(categoryName) && categoryName != "All")
        {
            query = query.Where(p => p.Category.Name.ToLower() == categoryName.ToLower());
        }

        if (minPrice.HasValue) query = query.Where(p => p.Price >= minPrice.Value);
        if (maxPrice.HasValue) query = query.Where(p => p.Price <= maxPrice.Value);

        // Fetch to memory for complex JSONB filtering (brands and dynamic specs)
        var productsList = await query.OrderByDescending(p => p.Id).ToListAsync();

        // 1. Filter Brands
        if (brands != null && brands.Any())
        {
            var brandSet = new HashSet<string>(brands.Select(b => b.ToLower()));
            productsList = productsList.Where(p => 
            {
                if (p.Specifications == null) return false;
                var root = p.Specifications.RootElement;
                if (root.TryGetProperty("brand", out var b1)) return brandSet.Contains(b1.ToString().ToLower());
                if (root.TryGetProperty("Brands", out var b2)) return brandSet.Contains(b2.ToString().ToLower());
                return false;
            }).ToList();
        }

        // 2. Filter Dynamic Specs
        if (!string.IsNullOrEmpty(specs))
        {
            try 
            {
                var specFilters = JsonSerializer.Deserialize<Dictionary<string, List<string>>>(specs);
                if (specFilters != null && specFilters.Any())
                {
                    productsList = productsList.Where(p => 
                    {
                        if (p.Specifications == null) return false;
                        var root = p.Specifications.RootElement;
                        
                        if (root.TryGetProperty("specifications", out var nestedSpecs) && nestedSpecs.ValueKind == JsonValueKind.Object)
                        {
                            root = nestedSpecs;
                        }
                        
                        // Must match ALL selected spec categories
                        foreach (var filter in specFilters)
                        {
                            var specKey = filter.Key;
                            var allowedValues = new HashSet<string>(filter.Value.Select(v => v.ToLower()));
                            
                            // Try to find the exact spec key ignoring case
                            var matchedProp = root.EnumerateObject().FirstOrDefault(prop => prop.Name.Equals(specKey, StringComparison.OrdinalIgnoreCase));
                            
                            if (matchedProp.Value.ValueKind == JsonValueKind.Undefined) return false; // Product doesn't have this spec
                            
                            var productSpecValue = matchedProp.Value.ToString().ToLower();
                            if (!allowedValues.Contains(productSpecValue)) return false;
                        }
                        return true;
                    }).ToList();
                }
            }
            catch { /* Ignore invalid JSON */ }
        }

        var total = productsList.Count;
        var totalPages = (int)Math.Ceiling(total / (double)limit);

        var pagedProducts = productsList
            .Skip((page - 1) * limit)
            .Take(limit)
            .Select(p => new ProductDto
            {
                Id = p.Id,
                Sku = p.Sku,
                Name = p.Name,
                Price = p.Price,
                Cost = p.Cost,
                OnHandQuantity = p.OnHandQuantity,
                ReservedQuantity = p.ReservedQuantity,
                AvailableQuantity = p.OnHandQuantity - p.ReservedQuantity,
                CategoryId = p.CategoryId,
                CategoryName = p.Category != null ? p.Category.Name : "N/A",
                ImageUrl = p.ImageUrl,
                Specifications = p.Specifications != null ? JsonDocument.Parse(p.Specifications.RootElement.GetRawText()).RootElement : null
            }).ToList();

        return Ok(new
        {
            success = true,
            total,
            page,
            totalPages,
            data = pagedProducts
        });
    }

    // GET: api/Products/filters
    [HttpGet("filters")]
    [AllowAnonymous]
    public async Task<IActionResult> GetFilterOptions([FromQuery] string? categoryName = null)
    {
        var query = _context.Products.Where(p => !p.IsDeleted);

        if (!string.IsNullOrEmpty(categoryName) && categoryName != "All")
        {
            query = query.Where(p => p.Category.Name.ToLower() == categoryName.ToLower());
        }

        var products = await query.Select(p => new { p.Price, p.Specifications }).ToListAsync();

        if (!products.Any())
        {
            return Ok(new
            {
                Brands = new List<string>(),
                AvailableSpecs = new Dictionary<string, List<string>>(),
                MinPrice = 0,
                MaxPrice = 0
            });
        }

        var brands = new HashSet<string>();
        var specsMap = new Dictionary<string, HashSet<string>>();
        decimal minPrice = products.Min(p => p.Price);
        decimal maxPrice = products.Max(p => p.Price);

        foreach (var p in products)
        {
            if (p.Specifications != null)
            {
                var specRoot = p.Specifications.RootElement;
                
                // บางสินค้าที่ Migrate มามี object ซ้อนอยู่ข้างในชื่อ specifications
                if (specRoot.TryGetProperty("specifications", out var nestedSpecs) && nestedSpecs.ValueKind == JsonValueKind.Object)
                {
                    specRoot = nestedSpecs;
                }
                
                foreach (var prop in specRoot.EnumerateObject())
                {
                    string key = prop.Name;
                    
                    // Skip internal fields or large objects
                    var ignoredKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase) 
                    { 
                        "__v", "tags", "brand", "brands", "image", "status", "createdAt", "updatedAt", 
                        "category", "description", "modelName", "isFeatured", "soldCount", "viewCount" 
                    };

                    if (key.StartsWith("_") || ignoredKeys.Contains(key) || prop.Value.ValueKind == JsonValueKind.Object || prop.Value.ValueKind == JsonValueKind.Array)
                        continue;

                    string value = prop.Value.ToString();
                    if (string.IsNullOrWhiteSpace(value)) continue;

                    if (key.ToLower() == "brand" || key.ToLower() == "brands")
                    {
                        brands.Add(value.ToUpper());
                        continue;
                    }

                    if (!specsMap.ContainsKey(key))
                    {
                        specsMap[key] = new HashSet<string>();
                    }
                    specsMap[key].Add(value);
                }
            }
        }

        // Convert HashSets to Lists and sort them
        var availableSpecs = specsMap.ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.OrderBy(v => v).ToList()
        );

        return Ok(new
        {
            Brands = brands.OrderBy(b => b).ToList(),
            AvailableSpecs = availableSpecs,
            MinPrice = minPrice,
            MaxPrice = maxPrice
        });
    }

    // GET: api/Products/5
    [HttpGet("{id}")]
    [AllowAnonymous]
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
            OnHandQuantity = product.OnHandQuantity,
            ReservedQuantity = product.ReservedQuantity,
            AvailableQuantity = product.OnHandQuantity - product.ReservedQuantity,
            Specifications = product.Specifications != null ? JsonDocument.Parse(product.Specifications.RootElement.GetRawText()).RootElement : null,
            CategoryId = product.CategoryId,
            CategoryName = product.Category != null ? product.Category.Name : "N/A",
            ImageUrl = product.ImageUrl
        };

        return Ok(dto);
    }

    // POST: api/Products
    [HttpPost]
    [Authorize(Roles = "Admin,Purchasing")] 
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
            CategoryId = dto.CategoryId,
            ImageUrl = dto.ImageUrl,
            CloudinaryPublicId = dto.CloudinaryPublicId
        };

        if (dto.Specifications.HasValue)
        {
            product.Specifications = JsonDocument.Parse(dto.Specifications.Value.GetRawText());
        }

        _context.Products.Add(product);
        await _context.SaveChangesAsync();

        return CreatedAtAction(nameof(GetProduct), new { id = product.Id }, new { message = "สร้างข้อมูลสินค้าสำเร็จ!" });
    }

    // POST: api/Products/upload-temp-image
    [HttpPost("upload-temp-image")]
    [Authorize(Roles = "Admin,Purchasing")]
    public async Task<IActionResult> UploadTempImage(IFormFile file)
    {
        var result = await _photoService.AddPhotoAsync(file);
        if (result.Error != null) return BadRequest(new { message = result.Error.Message });

        return Ok(new { 
            message = "อัปโหลดรูปภาพสำเร็จ",
            imageUrl = result.SecureUrl.AbsoluteUri,
            publicId = result.PublicId
        });
    }

    // PUT: api/Products/5
    [HttpPut("{id}")]
    [Authorize(Roles = "Admin,Purchasing")]
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

    // PATCH: api/Products/5/price
    [HttpPatch("{id}/price")]
    [Authorize(Roles = "Admin,Sales")]
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
    [Authorize(Roles = "Admin")]
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

    // POST: api/Products/5/image
    [HttpPost("{id}/image")]
    [Authorize(Roles = "Admin,Purchasing")]
    public async Task<IActionResult> AddProductImage(Guid id, IFormFile file)
    {
        var product = await _context.Products.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);
        if (product == null) return NotFound(new { message = "ไม่พบสินค้า" });

        var result = await _photoService.AddPhotoAsync(file);

        if (result.Error != null) return BadRequest(new { message = result.Error.Message });

        // Delete old photo if it exists
        if (!string.IsNullOrEmpty(product.CloudinaryPublicId))
        {
            await _photoService.DeletePhotoAsync(product.CloudinaryPublicId);
        }

        product.ImageUrl = result.SecureUrl.AbsoluteUri;
        product.CloudinaryPublicId = result.PublicId;

        await _context.SaveChangesAsync();

        return Ok(new { 
            message = "อัปโหลดรูปภาพสำเร็จ",
            imageUrl = product.ImageUrl 
        });
    }
}
