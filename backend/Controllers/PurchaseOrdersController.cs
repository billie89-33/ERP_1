using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Purchasing")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly JamineERP.Backend.Services.IGeminiAiService _aiService;

    public PurchaseOrdersController(AppDbContext context, JamineERP.Backend.Services.IGeminiAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    // GET: api/PurchaseOrders
    [HttpGet]
    public async Task<IActionResult> GetPurchaseOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "", [FromQuery] string status = "")
    {
        var query = _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.CreatedByUser)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.PoNumber.ToLower().Contains(search) || (p.Supplier != null && p.Supplier.CompanyName.ToLower().Contains(search)));
        }

        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status);
        }

        int totalCount = await query.CountAsync();

        var pos = await query
            .OrderByDescending(p => p.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.Id,
                p.PoNumber,
                p.OrderDate,
                p.Status,
                p.TotalAmount,
                SupplierName = p.Supplier.CompanyName,
                CreatedByName = p.CreatedByUser.Username
            })
            .ToListAsync();
            
        return Ok(new JamineERP.Backend.Models.Pagination.PaginatedResult<object>
        {
            Items = pos.Cast<object>().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
    }
    
    // GET: api/PurchaseOrders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetPurchaseOrder(Guid id)
    {
        var po = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (po == null) return NotFound();

        return Ok(new {
            po.Id,
            po.PoNumber,
            po.OrderDate,
            po.Status,
            po.SupplierId,
            SupplierName = po.Supplier.CompanyName,
            po.ExpectedDeliveryDate,
            po.Remarks,
            po.ReferenceNumber,
            po.PaymentTerms,
            po.SubTotal,
            po.VatRate,
            po.TaxAmount,
            po.TotalAmount,
            Items = po.PurchaseOrderItems.Select(i => new {
                i.ProductId,
                ProductName = i.Product.Name,
                i.Quantity,
                i.UnitCost
            })
        });
    }

    // POST: api/PurchaseOrders
    [HttpPost]
    public async Task<IActionResult> CreatePurchaseOrder(CreatePurchaseOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
        {
            return BadRequest(new { message = "กรุณาระบุสินค้าที่ต้องการสั่งซื้ออย่างน้อย 1 รายการ" });
        }

        // Generate PO Number using safe UTC day boundaries
        var today = DateTime.UtcNow;
        var startOfDay = DateTime.SpecifyKind(today.Date, DateTimeKind.Utc);
        var endOfDay = startOfDay.AddDays(1);
        var poCount = await _context.PurchaseOrders.CountAsync(po => po.OrderDate >= startOfDay && po.OrderDate < endOfDay);
        var poNumber = $"PO{today:yyyyMMdd}-{poCount + 1:D3}";

        // Calculate Total
        decimal subTotal = dto.Items.Sum(i => i.Quantity * i.UnitCost);
        decimal taxAmount = subTotal * (dto.VatRate / 100m);
        decimal totalAmount = subTotal + taxAmount;

        // Resolve authenticated user from JWT Claims first, fallback to first DB user
        Guid createdByUserId;
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && Guid.TryParse(userIdClaim, out var parsedUserId))
        {
            createdByUserId = parsedUserId;
        }
        else
        {
            var fallbackUser = await _context.Users.FirstOrDefaultAsync() ?? throw new Exception("No user found in DB for assignment.");
            createdByUserId = fallbackUser.Id;
        }

        var po = new PurchaseOrder
        {
            PoNumber = poNumber,
            OrderDate = today,
            Status = "Pending", // สถานะเริ่มต้นคือรอรับของ
            TotalAmount = totalAmount,
            SubTotal = subTotal,
            TaxAmount = taxAmount,
            VatRate = dto.VatRate,
            ExpectedDeliveryDate = dto.ExpectedDeliveryDate,
            Remarks = dto.Remarks,
            ReferenceNumber = dto.ReferenceNumber,
            PaymentTerms = dto.PaymentTerms,
            SupplierId = dto.SupplierId,
            CreatedByUserId = createdByUserId
        };

        foreach (var item in dto.Items)
        {
            po.PurchaseOrderItems.Add(new PurchaseOrderItem
            {
                ProductId = item.ProductId,
                Quantity = item.Quantity,
                UnitCost = item.UnitCost
            });
        }

        _context.PurchaseOrders.Add(po);
        await _context.SaveChangesAsync();
        await _aiService.TryEmbedPurchaseOrderAsync(_context, po.Id);
        await _aiService.TryEmbedSupplierAsync(_context, po.SupplierId);

        return Ok(new { message = "สร้างใบสั่งซื้อ (PO) สำเร็จ", poId = po.Id, poNumber = po.PoNumber });
    }

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelPurchaseOrder(Guid id)
    {
        var po = await _context.PurchaseOrders.FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

        if (po == null) return NotFound(new { message = "ไม่พบใบสั่งซื้อ" });

        if (po.Status != "Pending")
            return BadRequest(new { message = "สามารถยกเลิกได้เฉพาะบิลที่ยัง Pending เท่านั้น" });

        po.Status = "Cancelled";
        await _context.SaveChangesAsync();
        await _aiService.TryEmbedPurchaseOrderAsync(_context, po.Id);

        return Ok(new { message = "ยกเลิกใบสั่งซื้อสำเร็จ" });
    }
}
