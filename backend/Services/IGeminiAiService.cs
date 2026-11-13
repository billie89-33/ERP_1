using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;

namespace JamineERP.Backend.Services;

public interface IGeminiAiService
{
    bool IsConfigured { get; }
    Task<float[]?> GenerateEmbeddingAsync(string text);
    Task<string> GenerateChatResponseAsync(string systemInstruction, string userPrompt, List<ChatHistoryItemDto>? history = null);
    double ComputeCosineSimilarity(float[]? vectorA, float[]? vectorB);
    string ExtractSpecificationsText(JsonDocument? specifications);

    string BuildProductEmbeddingText(Product product, string? categoryName = null);
    string BuildSalesOrderEmbeddingText(SalesOrder so);
    string BuildPurchaseOrderEmbeddingText(PurchaseOrder po);
    string BuildSupplierEmbeddingText(Supplier supplier, IEnumerable<string>? suppliedProductNames = null);
    string BuildCustomerEmbeddingText(Customer customer, int orderCount = 0, decimal totalSpent = 0);

    Task<bool> TryEmbedProductAsync(AppDbContext context, Guid productId);
    Task<bool> TryEmbedSalesOrderAsync(AppDbContext context, Guid salesOrderId);
    Task<bool> TryEmbedPurchaseOrderAsync(AppDbContext context, Guid purchaseOrderId);
    Task<bool> TryEmbedSupplierAsync(AppDbContext context, Guid supplierId);
    Task<bool> TryEmbedCustomerAsync(AppDbContext context, Guid customerId);
}
