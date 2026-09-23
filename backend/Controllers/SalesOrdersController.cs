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
    [AllowAnonymous]
    [HttpGet("debug-count")]
    public async Task<IActionResult> DebugCount()
    {
        var count = await _context.SalesOrders.CountAsync();
        return Ok(new { Count = count });
    }

    [HttpGet("debug-user")]
    public IActionResult DebugUser()
    {
        var roles = User.Claims.Where(c => c.Type == System.Security.Claims.ClaimTypes.Role).Select(c => c.Value).ToList();
        var allClaims = User.Claims.Select(c => new { c.Type, c.Value }).ToList();
        return Ok(new { Roles = roles, AllClaims = allClaims, IdentityName = User.Identity?.Name, IsAuthenticated = User.Identity?.IsAuthenticated });
    }

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

    // GET: api/SalesOrders/{id}
    [HttpGet("{id}")]
    public async Task<IActionResult> GetSalesOrder(Guid id)
    {
        var so = await _context.SalesOrders
            .Include(s => s.Customer)
            .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

        if (so == null) return NotFound();

        return Ok(new {
            so.Id,
            so.OrderNumber,
            so.Status,
            CustomerName = so.Customer.CompanyName,
            Items = so.SalesOrderItems.Select(i => new {
                i.ProductId,
                ProductName = i.Product.Name,
                i.Quantity,
                i.UnitPrice
            })
        });
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
                Status = "Pending", // เปลี่ยนเป็น Pending เพราะต้องรอส่งของผ่าน GI
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

                var available = product.OnHandQuantity - product.ReservedQuantity;
                if (available < item.Quantity)
                    throw new Exception($"สินค้า '{product.Name}' มีสต็อกไม่พอจำหน่าย (พร้อมขาย {available}, ต้องการสั่ง {item.Quantity})");

                // เพิ่มยอดจอง (Reserved) สต็อกจริง (OnHand) จะไปตัดตอนออกใบ Goods Issue (GI)
                product.ReservedQuantity += item.Quantity;

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

    [HttpPut("{id}/cancel")]
    public async Task<IActionResult> CancelSalesOrder(Guid id)
    {
        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            var so = await _context.SalesOrders
                .Include(s => s.SalesOrderItems)
                .FirstOrDefaultAsync(s => s.Id == id && !s.IsDeleted);

            if (so == null) return NotFound(new { message = "ไม่พบบิลขาย" });

            if (so.Status != "Pending")
                return BadRequest(new { message = "สามารถยกเลิกได้เฉพาะบิลที่ยัง Pending เท่านั้น" });

            so.Status = "Cancelled";

            foreach (var item in so.SalesOrderItems)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product != null)
                {
                    // Return reserved stock
                    if (product.ReservedQuantity >= item.Quantity)
                    {
                        product.ReservedQuantity -= item.Quantity;
                    }
                    else
                    {
                        product.ReservedQuantity = 0; // Failsafe
                    }
                }
            }

            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { message = "ยกเลิกบิลขายและคืนสต๊อกสำเร็จ" });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "เกิดข้อผิดพลาดในการยกเลิกบิล", error = ex.Message });
        }
    }

    [HttpPost("{id}/verify-payment")]
    [Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> VerifyPayment(Guid id)
    {
        var so = await _context.SalesOrders.FindAsync(id);
        if (so == null || so.IsDeleted) return NotFound();

        if (so.PaymentStatus != "Checking")
            return BadRequest(new { message = "Order is not waiting for payment verification." });

        so.PaymentStatus = "Paid";
        await _context.SaveChangesAsync();
        return Ok(new { message = "Payment verified successfully." });
    }
}
