using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JamineERP.Backend.Models;

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int StockQuantity { get; set; }
    
    public JsonDocument? Specifications { get; set; }

    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
}
