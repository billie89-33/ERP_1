using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class Supplier : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string? ContactName { get; set; }
    public string? Phone { get; set; }
    public string? Email { get; set; }
    public string? Address { get; set; }
    public string? TaxId { get; set; }
    public string? PaymentTerms { get; set; }

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
