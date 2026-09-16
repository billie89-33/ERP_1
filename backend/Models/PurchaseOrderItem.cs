using System;

namespace JamineERP.Backend.Models;

public class PurchaseOrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }

    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
