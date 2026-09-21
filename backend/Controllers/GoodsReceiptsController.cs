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

public class CreateGoodsReceiptDto
{
    public Guid PurchaseOrderId { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<GoodsReceiptItemDto> Items { get; set; } = new();
}

public class GoodsReceiptItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Warehouse")]
public class GoodsReceiptsController : ControllerBase
{
    private readonly AppDbContext _context;

    public GoodsReceiptsController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetGoodsReceipts()
    {
        var grs = await _context.GoodsReceipts
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.ReceivedByUser)
            .Where(gr => !gr.IsDeleted)
            .OrderByDescending(gr => gr.ReceiptDate)
            .Select(gr => new {
                gr.Id,
                gr.GrNumber,
                gr.ReceiptDate,
                gr.Status,
                PoNumber = gr.PurchaseOrder.PoNumber,
                ReceivedByName = gr.ReceivedByUser.Username
            })
            .ToListAsync();
            
        return Ok(grs);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGoodsReceipt(CreateGoodsReceiptDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { message = "กรุณาระบุสินค้าที่รับเข้าอย่างน้อย 1 รายการ" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var po = await _context.PurchaseOrders
                .Include(p => p.PurchaseOrderItems)
                .FirstOrDefaultAsync(p => p.Id == dto.PurchaseOrderId);

            if (po == null)
                return NotFound(new { message = "ไม่พบใบสั่งซื้อที่ระบุ" });

            if (po.Status == "Received")
                return BadRequest(new { message = "ใบสั่งซื้อนี้รับของครบแล้ว" }); // Simplified for now

            // Check if user is logged in (Dummy user for now)
            var user = await _context.Users.FirstOrDefaultAsync() ?? throw new Exception("No user found.");

            var today = DateTime.UtcNow;
            var grCount = await _context.GoodsReceipts.CountAsync(gr => gr.ReceiptDate.Date == today.Date);
            var grNumber = $"GR{today:yyyyMMdd}-{grCount + 1:D3}";

            var gr = new GoodsReceipt
            {
                GrNumber = grNumber,
                ReceiptDate = today,
                Status = "Completed", // We will automatically complete it for simplicity
                Remarks = dto.Remarks,
                PurchaseOrderId = po.Id,
                ReceivedByUserId = user.Id
            };

            foreach (var itemDto in dto.Items)
            {
                var poItem = po.PurchaseOrderItems.FirstOrDefault(i => i.ProductId == itemDto.ProductId);
                if (poItem == null)
                    throw new Exception($"สินค้ารหัส {itemDto.ProductId} ไม่มีในใบสั่งซื้อ {po.PoNumber}");

                if (itemDto.Quantity <= 0)
                    throw new Exception("จำนวนรับเข้าต้องมากกว่า 0");

                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new Exception("ไม่พบสินค้า");

                // Update physical stock!
                product.OnHandQuantity += itemDto.Quantity;
                product.Cost = poItem.UnitCost; // Update latest cost

                gr.GoodsReceiptItems.Add(new GoodsReceiptItem
                {
                    ProductId = itemDto.ProductId,
                    ReceivedQuantity = itemDto.Quantity
                });
            }

            // Mark PO as received (In a real ERP, we would check if received qty == ordered qty)
            po.Status = "Received";

            _context.GoodsReceipts.Add(gr);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "รับสินค้าเข้าคลังสำเร็จ", grId = gr.Id, grNumber = gr.GrNumber });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "รับสินค้าเข้าคลังไม่สำเร็จ", error = ex.Message });
        }
    }
}
