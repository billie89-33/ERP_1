using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Purchasing")]
public class PurchaseOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public PurchaseOrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/PurchaseOrders
    [HttpGet]
    public async Task<IActionResult> GetPurchaseOrders()
    {
        var pos = await _context.PurchaseOrders
            .Include(p => p.Supplier)
            .Include(p => p.CreatedByUser)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.OrderDate)
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
            
        return Ok(pos);
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
            po.Status,
            SupplierName = po.Supplier.CompanyName,
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

        // Generate PO Number
        var today = DateTime.UtcNow;
        var poCount = await _context.PurchaseOrders.CountAsync(po => po.OrderDate.Date == today.Date);
        var poNumber = $"PO{today:yyyyMMdd}-{poCount + 1:D3}";

        // Calculate Total
        decimal totalAmount = dto.Items.Sum(i => i.Quantity * i.UnitCost);

        // Dummy User for now (Admin)
        var user = await _context.Users.FirstOrDefaultAsync() ?? throw new Exception("No user found in DB for assignment.");

        var po = new PurchaseOrder
        {
            PoNumber = poNumber,
            OrderDate = today,
            Status = "Pending", // สถานะเริ่มต้นคือรอรับของ
            TotalAmount = totalAmount,
            SupplierId = dto.SupplierId,
            CreatedByUserId = user.Id
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

        return Ok(new { message = "ยกเลิกใบสั่งซื้อสำเร็จ" });
    }
}
