using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class SalesOrder : BaseEntity
{
    public string OrderNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty; // "Pending, Shipped, Invoiced"
    public decimal TotalAmount { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    // E-Commerce specifics
    public DateTime? ExpiresAt { get; set; } 
    public string OrderChannel { get; set; } = "Web"; // Web, Admin

    // Payment Tracking
    public string PaymentStatus { get; set; } = "Pending"; // Pending, Checking, Paid
    public string PaymentSlipUrl { get; set; } = string.Empty;

    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
}
