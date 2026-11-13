using System;
using System.Collections.Generic;
using System.Text.Json;

namespace JamineERP.Backend.DTOs;

public class AiModuleCountDto
{
    public int Total { get; set; }
    public int Embedded { get; set; }
    public int Pending => Math.Max(0, Total - Embedded);
}

public class AiStatusDto
{
    public bool ApiKeyConfigured { get; set; }
    public string EmbeddingModel { get; set; } = "gemini-embedding-001";
    public string ChatModel { get; set; } = "gemini-2.5-flash";
    public AiModuleCountDto Products { get; set; } = new();
    public AiModuleCountDto SalesOrders { get; set; } = new();
    public AiModuleCountDto PurchaseOrders { get; set; } = new();
    public AiModuleCountDto Suppliers { get; set; } = new();
    public AiModuleCountDto Customers { get; set; } = new();
    public int TotalRecords => Products.Total + SalesOrders.Total + PurchaseOrders.Total + Suppliers.Total + Customers.Total;
    public int TotalEmbedded => Products.Embedded + SalesOrders.Embedded + PurchaseOrders.Embedded + Suppliers.Embedded + Customers.Embedded;
}

public class SyncEmbeddingsRequestDto
{
    public bool ForceRebuildAll { get; set; } = false;
    public string? Module { get; set; } // Optional: "products", "sales-orders", "purchase-orders", "suppliers", "customers", or null/"all"
}

public class SyncEmbeddingsResponseDto
{
    public string Message { get; set; } = string.Empty;
    public int ProductsSynced { get; set; }
    public int SalesOrdersSynced { get; set; }
    public int PurchaseOrdersSynced { get; set; }
    public int SuppliersSynced { get; set; }
    public int CustomersSynced { get; set; }
    public int TotalSynced => ProductsSynced + SalesOrdersSynced + PurchaseOrdersSynced + SuppliersSynced + CustomersSynced;
    public int FailedCount { get; set; }
    public string? LastError { get; set; }
}

public class SemanticSearchRequestDto
{
    public string Query { get; set; } = string.Empty;
    public int TopK { get; set; } = 12;
    public bool OnlyInStock { get; set; } = false;
    public string? CategoryName { get; set; }
}

public class SemanticProductMatchDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public string? ImageUrl { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public JsonElement? Specifications { get; set; }
    public double SimilarityScore { get; set; }
}

public class DuplicateCheckRequestDto
{
    public string Name { get; set; } = string.Empty;
    public string? Brand { get; set; }
    public string? ModelName { get; set; }
    public string? Sku { get; set; }
    public double Threshold { get; set; } = 0.80;
}

public class DuplicateCheckResponseDto
{
    public bool HasDuplicates { get; set; }
    public List<SemanticProductMatchDto> Matches { get; set; } = new();
}

public class ChatHistoryItemDto
{
    public string Role { get; set; } = "user"; // "user" or "model"
    public string Text { get; set; } = string.Empty;
}

public class ChatRequestDto
{
    public string Message { get; set; } = string.Empty;
    public List<ChatHistoryItemDto> History { get; set; } = new();
}

public class ChatDocumentRefDto
{
    public string Type { get; set; } = string.Empty; // "SO", "PO", "Product", "Supplier", "Customer"
    public Guid Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public string Route { get; set; } = string.Empty;
    public double SimilarityScore { get; set; }
}

public class ChatResponseDto
{
    public string Reply { get; set; } = string.Empty;
    public List<SemanticProductMatchDto> SuggestedProducts { get; set; } = new();
    public List<ChatDocumentRefDto> ReferencedDocuments { get; set; } = new();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}

public class DashboardAiBriefDto
{
    public string FinancialSummary { get; set; } = string.Empty;
    public string UrgentActions { get; set; } = string.Empty;
    public string RestockRecommendations { get; set; } = string.Empty;
    public string ExecutiveTakeaway { get; set; } = string.Empty;
    public decimal TotalRevenue { get; set; }
    public decimal TotalExpense { get; set; }
    public decimal NetCashflow => TotalRevenue - TotalExpense;
    public int PendingInboundPOs { get; set; }
    public int PendingOutboundSOs { get; set; }
    public int PendingSlipVerifications { get; set; }
    public DateTime GeneratedAt { get; set; } = DateTime.UtcNow;
}
