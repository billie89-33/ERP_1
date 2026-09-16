using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class PurchaseOrder : BaseEntity
{
    public string PoNumber { get; set; } = string.Empty;
    public DateTime OrderDate { get; set; }
    public string Status { get; set; } = string.Empty; // "Pending, Received"
    public decimal TotalAmount { get; set; }

    public Guid SupplierId { get; set; }
    public Supplier Supplier { get; set; } = null!;

    public Guid CreatedByUserId { get; set; }
    public User CreatedByUser { get; set; } = null!;

    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
