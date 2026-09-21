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

public class CreateGoodsIssueDto
{
    public Guid SalesOrderId { get; set; }
    public string Remarks { get; set; } = string.Empty;
    public List<GoodsIssueItemDto> Items { get; set; } = new();
}

public class GoodsIssueItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin,Warehouse")]
public class GoodsIssuesController : ControllerBase
{
    private readonly AppDbContext _context;

    public GoodsIssuesController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetGoodsIssues()
    {
        var gis = await _context.GoodsIssues
            .Include(gi => gi.SalesOrder)
            .Include(gi => gi.IssuedByUser)
            .Where(gi => !gi.IsDeleted)
            .OrderByDescending(gi => gi.IssueDate)
            .Select(gi => new {
                gi.Id,
                gi.GiNumber,
                gi.IssueDate,
                gi.Status,
                SoNumber = gi.SalesOrder.OrderNumber,
                IssuedByName = gi.IssuedByUser.Username
            })
            .ToListAsync();
            
        return Ok(gis);
    }

    [HttpPost]
    public async Task<IActionResult> CreateGoodsIssue(CreateGoodsIssueDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { message = "กรุณาระบุสินค้าที่ตัดจ่ายอย่างน้อย 1 รายการ" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var so = await _context.SalesOrders
                .Include(s => s.SalesOrderItems)
                .FirstOrDefaultAsync(s => s.Id == dto.SalesOrderId);

            if (so == null)
                return NotFound(new { message = "ไม่พบใบสั่งขายที่ระบุ" });

            if (so.Status == "Shipped")
                return BadRequest(new { message = "ใบสั่งขายนี้ถูกตัดสต็อกไปแล้ว" });

            // Dummy user
            var user = await _context.Users.FirstOrDefaultAsync() ?? throw new Exception("No user found.");

            var today = DateTime.UtcNow;
            var giCount = await _context.GoodsIssues.CountAsync(gi => gi.IssueDate.Date == today.Date);
            var giNumber = $"GI{today:yyyyMMdd}-{giCount + 1:D3}";

            var gi = new GoodsIssue
            {
                GiNumber = giNumber,
                IssueDate = today,
                Status = "Completed",
                Remarks = dto.Remarks,
                SalesOrderId = so.Id,
                IssuedByUserId = user.Id
            };

            foreach (var itemDto in dto.Items)
            {
                var soItem = so.SalesOrderItems.FirstOrDefault(i => i.ProductId == itemDto.ProductId);
                if (soItem == null)
                    throw new Exception($"สินค้ารหัส {itemDto.ProductId} ไม่มีในใบสั่งขาย {so.OrderNumber}");

                if (itemDto.Quantity <= 0)
                    throw new Exception("จำนวนตัดจ่ายต้องมากกว่า 0");

                var product = await _context.Products.FindAsync(itemDto.ProductId);
                if (product == null)
                    throw new Exception("ไม่พบสินค้า");

                // ERP Core Logic: Deduct actual physical stock AND clear the reservation!
                if (product.OnHandQuantity < itemDto.Quantity)
                    throw new Exception($"สต็อกจริงมีไม่พอให้ตัดจ่าย! (สินค้า: {product.Name}, มี: {product.OnHandQuantity}, จะตัด: {itemDto.Quantity})");

                if (product.ReservedQuantity < itemDto.Quantity)
                    throw new Exception($"ยอดจองผิดพลาด! ไม่มียอดจองเหลือให้ตัดจ่ายสำหรับสินค้านี้ (สินค้า: {product.Name})");

                product.OnHandQuantity -= itemDto.Quantity;
                product.ReservedQuantity -= itemDto.Quantity;

                gi.GoodsIssueItems.Add(new GoodsIssueItem
                {
                    ProductId = itemDto.ProductId,
                    IssuedQuantity = itemDto.Quantity
                });
            }

            // Update SO Status
            so.Status = "Shipped";

            _context.GoodsIssues.Add(gi);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "ตัดสินค้าออกจากคลังสำเร็จ", giId = gi.Id, giNumber = gi.GiNumber });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "ตัดสินค้าออกจากคลังไม่สำเร็จ", error = ex.Message });
        }
    }
}
