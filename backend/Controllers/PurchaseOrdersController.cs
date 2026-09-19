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

    // POST: api/PurchaseOrders/{id}/receive
    [HttpPost("{id}/receive")]
    public async Task<IActionResult> ReceivePurchaseOrder(Guid id)
    {
        // 🚀 ไฮไลท์: ใช้ Transaction ในการรับของเข้าสต๊อก
        using var transaction = await _context.Database.BeginTransactionAsync();
        
        try
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == id && !p.IsDeleted);

            if (po == null)
                return NotFound(new { message = "ไม่พบใบสั่งซื้อที่ระบุ" });

            if (po.Status == "Received")
                return BadRequest(new { message = "ใบสั่งซื้อนี้รับของเข้าระบบไปแล้ว!" });

            // 1. Update PO Status
            po.Status = "Received";

            // 2. Loop update product stock
            foreach (var item in po.PurchaseOrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new Exception($"ไม่พบสินค้า ProductId: {item.ProductId} ในระบบ ข้อมูลอาจเสียหาย");

                // 🌟 +เพิ่มสต๊อก (และอาจจะอัปเดตราคาต้นทุนเฉลี่ย (Moving Average) ได้ที่นี่)
                product.StockQuantity += item.Quantity;
                product.Cost = item.UnitCost; // อัปเดตต้นทุนล่าสุดง่ายๆ
            }

            // 3. Save Changes
            await _context.SaveChangesAsync();

            // 4. Commit Transaction
            await transaction.CommitAsync();

            return Ok(new { message = $"รับสินค้าเข้าระบบสำเร็จ สต๊อกอัปเดตแล้ว (PO: {po.PoNumber})" });
        }
        catch (Exception ex)
        {
            // 🚨 ถ้าพังตรงไหนก็ตาม (เช่น ไฟดับ, DB ล่ม, โค้ดผิด) ข้อมูลทั้งหมดจะถูก Rollback กลับคืนเหมือนไม่มีอะไรเกิดขึ้น!
            await transaction.RollbackAsync();
            return StatusCode(500, new { message = "เกิดข้อผิดพลาดขณะรับของเข้าระบบ การทำรายการถูกยกเลิก (Rollback)", error = ex.Message });
        }
    }
}
