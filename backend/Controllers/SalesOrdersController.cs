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
[Authorize(Roles = "Admin,Sales")]
public class SalesOrdersController : ControllerBase
{
    private readonly AppDbContext _context;

    public SalesOrdersController(AppDbContext context)
    {
        _context = context;
    }

    // GET: api/SalesOrders
    [HttpGet]
    public async Task<IActionResult> GetSalesOrders()
    {
        var sos = await _context.SalesOrders
            .Include(p => p.Customer)
            .Include(p => p.CreatedByUser)
            .Where(p => !p.IsDeleted)
            .OrderByDescending(p => p.OrderDate)
            .Select(p => new {
                p.Id,
                p.OrderNumber,
                p.OrderDate,
                p.Status,
                p.TotalAmount,
                CustomerName = p.Customer.CompanyName,
                CreatedByName = p.CreatedByUser.Username
            })
            .ToListAsync();
            
        return Ok(sos);
    }

    // POST: api/SalesOrders
    // ไฮไลท์: เปิดบิลขาย (สร้าง SO) จะทำการ "ตัดสต๊อก" ทันทีด้วย Transaction
    [HttpPost]
    public async Task<IActionResult> CreateSalesOrder(CreateSalesOrderDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { message = "กรุณาระบุสินค้าที่ต้องการขายอย่างน้อย 1 รายการ" });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Generate SO Number
            var today = DateTime.UtcNow;
            var soCount = await _context.SalesOrders.CountAsync(so => so.OrderDate.Date == today.Date);
            var OrderNumber = $"SO{today:yyyyMMdd}-{soCount + 1:D3}";

            decimal totalAmount = dto.Items.Sum(i => i.Quantity * i.UnitPrice);

            // Dummy User (Admin)
            var user = await _context.Users.FirstOrDefaultAsync() ?? throw new Exception("No user found.");

            var so = new SalesOrder
            {
                OrderNumber = OrderNumber,
                OrderDate = today,
                Status = "Completed", // สมมติว่าขายหน้าร้าน ตัดสต๊อกและเสร็จสิ้นทันที
                TotalAmount = totalAmount,
                CustomerId = dto.CustomerId,
                CreatedByUserId = user.Id
            };

            foreach (var item in dto.Items)
            {
                // ตรวจสอบสต๊อกสินค้า
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new Exception($"ไม่พบสินค้า (ID: {item.ProductId}) ในระบบ");

                if (product.StockQuantity < item.Quantity)
                    throw new Exception($"สินค้า '{product.Name}' มีสต๊อกไม่พอ (เหลือ {product.StockQuantity}, ต้องการ {item.Quantity})");

                // 🌟 -หักสต๊อก
                product.StockQuantity -= item.Quantity;

                so.SalesOrderItems.Add(new SalesOrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = item.UnitPrice
                });
            }

            _context.SalesOrders.Add(so);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "สร้างบิลขายสำเร็จ สต๊อกถูกตัดเรียบร้อย", soId = so.Id, OrderNumber = so.OrderNumber });
        }
        catch (Exception ex)
        {
            // 🚨 ถ้าพัง (เช่น สต๊อกไม่พอ) บิลขายจะไม่ถูกสร้าง และสต๊อกจะไม่ถูกตัด (Rollback)
            await transaction.RollbackAsync();
            return BadRequest(new { message = "ไม่สามารถสร้างบิลขายได้ การทำรายการถูกยกเลิก", error = ex.Message });
        }
    }
}
