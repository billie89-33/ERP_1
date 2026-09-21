using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class GoodsIssue : BaseEntity
{
    public string GiNumber { get; set; } = string.Empty;
    public DateTime IssueDate { get; set; }
    public string Status { get; set; } = "Pending"; // "Pending", "Shipped"
    public string? Remarks { get; set; }

    // Relationship to Sales Order
    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    // Picker / Warehouse Staff
    public Guid IssuedByUserId { get; set; }
    public User IssuedByUser { get; set; } = null!;

    public ICollection<GoodsIssueItem> GoodsIssueItems { get; set; } = new List<GoodsIssueItem>();
}
