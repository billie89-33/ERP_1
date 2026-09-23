using System;
using System.Collections.Generic;
using System.Text.Json;

using System.ComponentModel.DataAnnotations.Schema;
using Microsoft.EntityFrameworkCore;

namespace JamineERP.Backend.Models;

[Owned]
public class ProductImage
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}

public class Product : BaseEntity
{
    public string Sku { get; set; } = string.Empty;
    
    // Name is kept for quick display/fallback, but Brand & ModelName are the core
    public string Name { get; set; } = string.Empty; 
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    public decimal Price { get; set; }
    public decimal Cost { get; set; } // Kept for ERP cost tracking

    public ProductImage Image { get; set; } = new ProductImage();

    public string[] Tags { get; set; } = Array.Empty<string>();
    
    public string Status { get; set; } = "ACTIVE";
    public bool IsFeatured { get; set; } = false;
    public int SoldCount { get; set; } = 0;
    public int ViewCount { get; set; } = 0;
    
    // ERP Inventory Tracking
    public int OnHandQuantity { get; set; } = 0;
    public int ReservedQuantity { get; set; } = 0;
    
    public JsonDocument? Specifications { get; set; }
    
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
    public ICollection<SalesOrderItem> SalesOrderItems { get; set; } = new List<SalesOrderItem>();
    public ICollection<PurchaseOrderItem> PurchaseOrderItems { get; set; } = new List<PurchaseOrderItem>();
    public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
    public ICollection<GoodsIssueItem> GoodsIssueItems { get; set; } = new List<GoodsIssueItem>();
}
