using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using JamineERP.Backend.Data;
using System.Linq;
using System.Threading.Tasks;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize(Roles = "Admin")]
public class DashboardController : ControllerBase
{
    private readonly AppDbContext _context;

    public DashboardController(AppDbContext context)
    {
        _context = context;
    }

    [HttpGet("stats")]
    public async Task<IActionResult> GetStats()
    {
        var totalSales = await _context.SalesOrders
            .Where(so => !so.IsDeleted && so.Status != "Cancelled")
            .SumAsync(so => so.TotalAmount);

        var pendingSoCount = await _context.SalesOrders
            .CountAsync(so => !so.IsDeleted && so.Status == "Pending");

        var pendingSlipCount = await _context.SalesOrders
            .CountAsync(so => !so.IsDeleted && so.PaymentStatus == "Checking");

        var pendingPoCount = await _context.PurchaseOrders
            .CountAsync(po => !po.IsDeleted && po.Status == "Pending");

        var lowStockProducts = await _context.Products
            .Where(p => !p.IsDeleted && p.OnHandQuantity <= 5)
            .OrderBy(p => p.OnHandQuantity)
            .Take(5)
            .Select(p => new { p.Sku, p.Name, p.OnHandQuantity })
            .ToListAsync();

        // Generate past 7 days for the chart
        var past7Days = Enumerable.Range(0, 7)
            .Select(i => DateTime.UtcNow.Date.AddDays(-6 + i))
            .ToList();

        // Calculate sales per day (client-side grouping because EF Core might struggle with Date translation)
        var recentSales = await _context.SalesOrders
            .Where(so => !so.IsDeleted && so.Status != "Cancelled" && so.CreatedAt >= DateTime.UtcNow.AddDays(-7))
            .Select(so => new { so.CreatedAt, so.TotalAmount })
            .ToListAsync();

        var chartData = past7Days.Select(date => new {
            Date = date.ToString("dd MMM"),
            Amount = recentSales.Where(s => s.CreatedAt.Date == date).Sum(s => s.TotalAmount)
        }).ToList();

        // Get recent events (Mocking with SalesOrders for now)
        var recentEvents = await _context.SalesOrders
            .Where(so => !so.IsDeleted)
            .OrderByDescending(so => so.CreatedAt)
            .Take(5)
            .Select(so => new {
                Timestamp = so.CreatedAt.ToString("HH:mm:ss"),
                EventId = so.OrderNumber,
                Type = "ORDER_CREATE",
                Details = "Total: " + so.TotalAmount + " THB",
                Status = so.Status
            })
            .ToListAsync();

        return Ok(new
        {
            TotalSales = totalSales,
            PendingSalesOrders = pendingSoCount,
            PendingSlipVerifications = pendingSlipCount,
            PendingPurchaseOrders = pendingPoCount,
            LowStockProducts = lowStockProducts,
            ChartData = chartData,
            RecentEvents = recentEvents
        });
    }
}
