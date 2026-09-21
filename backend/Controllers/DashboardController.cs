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

        var pendingPoCount = await _context.PurchaseOrders
            .CountAsync(po => !po.IsDeleted && po.Status == "Pending");

        var lowStockProducts = await _context.Products
            .Where(p => !p.IsDeleted && p.OnHandQuantity <= 5)
            .OrderBy(p => p.OnHandQuantity)
            .Take(5)
            .Select(p => new { p.Sku, p.Name, p.OnHandQuantity })
            .ToListAsync();

        return Ok(new
        {
            TotalSales = totalSales,
            PendingSalesOrders = pendingSoCount,
            PendingPurchaseOrders = pendingPoCount,
            LowStockProducts = lowStockProducts
        });
    }
}
