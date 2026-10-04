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
    public async Task<IActionResult> GetSalesOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string search = "", [FromQuery] string status = "")
    {
        var query = _context.SalesOrders
            .Include(p => p.Customer)
            .Include(p => p.CreatedByUser)
            .Where(p => !p.IsDeleted)
            .AsQueryable();

        if (!string.IsNullOrEmpty(search))
        {
            search = search.ToLower();
            query = query.Where(p => p.OrderNumber.ToLower().Contains(search) || (p.Customer != null && p.Customer.CompanyName.ToLower().Contains(search)));
        }
        
        if (!string.IsNullOrEmpty(status))
        {
            query = query.Where(p => p.Status == status || p.PaymentStatus == status);
        }

        int totalCount = await query.CountAsync();

        var sos = await query
            .OrderByDescending(p => p.OrderDate)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(p => new {
                p.Id,
                p.OrderNumber,
                p.OrderDate,
                p.Status,
                p.PaymentStatus,
                p.OrderChannel,
                p.TotalAmount,
                CustomerName = p.Customer.CompanyName,
                CreatedByName = p.CreatedByUser.Username
            })
            .ToListAsync();
            
        return Ok(new JamineERP.Backend.Models.Pagination.PaginatedResult<object>
        {
            Items = sos.Cast<object>().ToList(),
            TotalCount = totalCount,
            Page = page,
            PageSize = pageSize
        });
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

    [HttpGet("export")]
    [Authorize(Roles = "Admin,Sales")]
    public async Task<IActionResult> ExportSalesOrders()
    {
        var orders = await _context.SalesOrders
            .Include(so => so.Customer)
            .Where(so => !so.IsDeleted)
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync();

        using var workbook = new ClosedXML.Excel.XLWorkbook();
        var worksheet = workbook.Worksheets.Add("Sales Orders");
        var currentRow = 1;

        worksheet.Cell(currentRow, 1).Value = "Order Number";
        worksheet.Cell(currentRow, 2).Value = "Order Date";
        worksheet.Cell(currentRow, 3).Value = "Customer";
        worksheet.Cell(currentRow, 4).Value = "Amount";
        worksheet.Cell(currentRow, 5).Value = "Status";
        worksheet.Cell(currentRow, 6).Value = "Payment Status";

        foreach (var order in orders)
        {
            currentRow++;
            worksheet.Cell(currentRow, 1).Value = order.OrderNumber;
            worksheet.Cell(currentRow, 2).Value = order.OrderDate.ToString("yyyy-MM-dd HH:mm");
            worksheet.Cell(currentRow, 3).Value = order.Customer?.CompanyName ?? "Unknown";
            worksheet.Cell(currentRow, 4).Value = order.TotalAmount;
            worksheet.Cell(currentRow, 5).Value = order.Status;
            worksheet.Cell(currentRow, 6).Value = order.PaymentStatus;
        }

        worksheet.Columns().AdjustToContents();

        using var stream = new System.IO.MemoryStream();
        workbook.SaveAs(stream);
        var content = stream.ToArray();

        return File(content, "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet", "SalesOrders.xlsx");
    }
}
