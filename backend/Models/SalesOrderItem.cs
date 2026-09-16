using System;

namespace JamineERP.Backend.Models;

public class SalesOrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }

    public Guid SalesOrderId { get; set; }
    public SalesOrder SalesOrder { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
