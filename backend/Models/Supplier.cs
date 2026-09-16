using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class Supplier : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string ContactName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;

    public ICollection<PurchaseOrder> PurchaseOrders { get; set; } = new List<PurchaseOrder>();
}
