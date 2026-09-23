using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.EntityFrameworkCore;
using JamineERP.Backend.Data;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace JamineERP.Backend.Services;

public class OrderExpiryWorker : BackgroundService
{
    private readonly ILogger<OrderExpiryWorker> _logger;
    private readonly IServiceProvider _serviceProvider;

    public OrderExpiryWorker(ILogger<OrderExpiryWorker> logger, IServiceProvider serviceProvider)
    {
        _logger = logger;
        _serviceProvider = serviceProvider;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Order Expiry Worker is starting.");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredOrdersAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error occurred processing expired orders.");
            }

            // Run every 1 minute
            await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
        }

        _logger.LogInformation("Order Expiry Worker is stopping.");
    }

    private async Task ProcessExpiredOrdersAsync(CancellationToken stoppingToken)
    {
        // Scope is required since AppDbContext is scoped
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var now = DateTime.UtcNow;

        // Find orders that are pending payment, have an expiration date, and have expired
        var expiredOrders = await context.SalesOrders
            .Include(so => so.SalesOrderItems)
            .ThenInclude(i => i.Product)
            .Where(so => !so.IsDeleted 
                      && so.PaymentStatus == "Pending" 
                      && so.Status != "Cancelled" 
                      && so.ExpiresAt.HasValue 
                      && so.ExpiresAt.Value <= now)
            .ToListAsync(stoppingToken);

        if (!expiredOrders.Any())
            return;

        _logger.LogInformation($"Found {expiredOrders.Count} expired order(s). Cancelling them now.");

        foreach (var order in expiredOrders)
        {
            order.Status = "Cancelled";
            order.PaymentStatus = "Expired";
            
            // Release Stock Reservation
            foreach (var item in order.SalesOrderItems)
            {
                if (item.Product != null)
                {
                    item.Product.ReservedQuantity -= item.Quantity;
                    // Ensure it does not go below 0 just in case
                    if (item.Product.ReservedQuantity < 0) 
                        item.Product.ReservedQuantity = 0;
                }
            }
        }

        await context.SaveChangesAsync(stoppingToken);
        _logger.LogInformation($"Successfully cancelled {expiredOrders.Count} expired order(s) and released stock.");
    }
}

