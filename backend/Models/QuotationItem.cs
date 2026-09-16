using System;

namespace JamineERP.Backend.Models;

public class QuotationItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public decimal Discount { get; set; }

    public Guid QuotationId { get; set; }
    public Quotation Quotation { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
}
