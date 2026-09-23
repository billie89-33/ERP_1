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

public class StorefrontCheckoutDto
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public List<StorefrontCheckoutItemDto> Items { get; set; } = new();
}

public class StorefrontCheckoutItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
}

[Route("api/[controller]")]
[ApiController]
[AllowAnonymous] // Anyone can checkout on the storefront
public class StorefrontController : ControllerBase
{
    private readonly AppDbContext _context;

    public StorefrontController(AppDbContext context)
    {
        _context = context;
    }

    [HttpPost("checkout")]
    public async Task<IActionResult> Checkout(StorefrontCheckoutDto dto)
    {
        if (dto.Items == null || !dto.Items.Any())
            return BadRequest(new { message = "Cart is empty." });

        using var transaction = await _context.Database.BeginTransactionAsync();
        try
        {
            // 1. Create or Find Customer (B2C)
            // For now, we just create a new customer record for each checkout or match by Email
            var customer = await _context.Customers.FirstOrDefaultAsync(c => c.TaxId == dto.Email); // Using TaxId to store email for B2C temporarily
            if (customer == null)
            {
                customer = new Customer
                {
                    CompanyName = $"{dto.FirstName} {dto.LastName}",
                    TaxId = dto.Email, // Store email here
                    Address = dto.Address,
                    CreditTermDays = 0,
                    CreditLimit = 0
                };
                _context.Customers.Add(customer);
                await _context.SaveChangesAsync();
            }
            else
            {
                // Update address if changed
                customer.Address = dto.Address;
                customer.CompanyName = $"{dto.FirstName} {dto.LastName}";
            }

            // 2. Generate SO Number
            var today = DateTime.UtcNow;
            var soCount = await _context.SalesOrders.CountAsync(so => so.OrderDate.Date == today.Date);
            var orderNumber = $"WEB{today:yyyyMMdd}-{soCount + 1:D3}";

            // We need a system user to associate as "CreatedBy" for web orders
            var systemUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (systemUser == null)
                throw new Exception("System user not found.");

            var so = new SalesOrder
            {
                OrderNumber = orderNumber,
                OrderDate = today,
                Status = "Pending", // B2C orders start as Pending, awaiting payment or packing
                CustomerId = customer.Id,
                CreatedByUserId = systemUser.Id,
                TotalAmount = 0, // Will calculate below
                OrderChannel = "Web",
                ExpiresAt = today.AddMinutes(10)
            };

            decimal totalAmount = 0;

            foreach (var item in dto.Items)
            {
                var product = await _context.Products.FindAsync(item.ProductId);
                if (product == null)
                    throw new Exception($"Product ID: {item.ProductId} not found.");

                // Check Available Stock (OnHand - Reserved)
                var available = product.OnHandQuantity - product.ReservedQuantity;
                if (available < item.Quantity)
                    throw new Exception($"Product '{product.Name}' does not have enough stock (Available: {available}, Requested: {item.Quantity}).");

                // Reserve the stock!
                product.ReservedQuantity += item.Quantity;

                // Use DB price, NEVER trust client price
                var unitPrice = product.Price;
                totalAmount += unitPrice * item.Quantity;

                so.SalesOrderItems.Add(new SalesOrderItem
                {
                    ProductId = item.ProductId,
                    Quantity = item.Quantity,
                    UnitPrice = unitPrice
                });
            }

            so.TotalAmount = totalAmount;

            _context.SalesOrders.Add(so);
            await _context.SaveChangesAsync();
            await transaction.CommitAsync();

            return Ok(new { 
                message = "Checkout successful! Your order has been placed.", 
                orderNumber = so.OrderNumber,
                totalAmount = so.TotalAmount
            });
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            return BadRequest(new { message = "Checkout failed.", error = ex.Message });
        }
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("my-orders")]
    public async Task<IActionResult> GetMyOrders()
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) return NotFound(new { message = "Customer profile not found." });

        var orders = await _context.SalesOrders
            .Where(so => so.CustomerId == customer.Id && !so.IsDeleted)
            .OrderByDescending(so => so.OrderDate)
            .Select(so => new
            {
                so.Id,
                so.OrderNumber,
                so.OrderDate,
                so.Status,
                so.PaymentStatus,
                so.TotalAmount,
                so.ExpiresAt
            })
            .ToListAsync();

        return Ok(orders);
    }

    [Authorize(Roles = "Customer")]
    [HttpGet("orders/{id}")]
    public async Task<IActionResult> GetOrderDetails(Guid id)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) return Unauthorized();

        var order = await _context.SalesOrders
            .Include(so => so.SalesOrderItems)
                .ThenInclude(i => i.Product)
            .FirstOrDefaultAsync(so => so.Id == id && so.CustomerId == customer.Id && !so.IsDeleted);

        if (order == null) return NotFound();

        return Ok(new
        {
            order.Id,
            order.OrderNumber,
            order.OrderDate,
            order.Status,
            order.PaymentStatus,
            order.PaymentSlipUrl,
            order.TotalAmount,
            order.ExpiresAt,
            Items = order.SalesOrderItems.Select(i => new
            {
                i.ProductId,
                ProductName = i.Product.Name,
                i.Quantity,
                i.UnitPrice
            })
        });
    }

    public class UploadSlipDto
    {
        public string Base64Image { get; set; } = string.Empty;
    }

    [Authorize(Roles = "Customer")]
    [HttpPost("orders/{id}/upload-slip")]
    public async Task<IActionResult> UploadSlip(Guid id, [FromBody] UploadSlipDto dto)
    {
        var userIdString = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdString) || !Guid.TryParse(userIdString, out var userId))
            return Unauthorized();

        var customer = await _context.Customers.FirstOrDefaultAsync(c => c.UserId == userId);
        if (customer == null) return Unauthorized();

        var order = await _context.SalesOrders
            .FirstOrDefaultAsync(so => so.Id == id && so.CustomerId == customer.Id && !so.IsDeleted);

        if (order == null) return NotFound();

        if (order.Status == "Cancelled")
            return BadRequest(new { message = "Order has been cancelled." });

        if (order.ExpiresAt.HasValue && DateTime.UtcNow > order.ExpiresAt.Value)
        {
            return BadRequest(new { message = "Order has expired. Please create a new order." });
        }

        if (string.IsNullOrEmpty(dto.Base64Image))
            return BadRequest(new { message = "Image is required." });

        order.PaymentSlipUrl = dto.Base64Image;
        order.PaymentStatus = "Checking";

        await _context.SaveChangesAsync();
        return Ok(new { message = "Slip uploaded successfully." });
    }
}
