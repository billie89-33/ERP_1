using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace JamineERP.Backend.Services;

public class GeminiAiService : IGeminiAiService
{
    private readonly HttpClient _httpClient;
    private readonly IConfiguration _configuration;
    private readonly ILogger<GeminiAiService> _logger;

    public GeminiAiService(HttpClient httpClient, IConfiguration configuration, ILogger<GeminiAiService> logger)
    {
        _httpClient = httpClient;
        _configuration = configuration;
        _logger = logger;
    }

    private string GetApiKey()
    {
        return Environment.GetEnvironmentVariable("GEMINI_API_KEY")
            ?? Environment.GetEnvironmentVariable("Gemini__ApiKey")
            ?? _configuration["Gemini:ApiKey"]
            ?? string.Empty;
    }

    public bool IsConfigured => !string.IsNullOrWhiteSpace(GetApiKey());

    public async Task<float[]?> GenerateEmbeddingAsync(string text)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey) || string.IsNullOrWhiteSpace(text))
            return null;

        var modelsToTry = new[] { "gemini-embedding-001", "text-embedding-004" };
        foreach (var modelName in modelsToTry)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:embedContent?key={apiKey}";
                var payload = new
                {
                    model = $"models/{modelName}",
                    content = new
                    {
                        parts = new[] { new { text } }
                    },
                    outputDimensionality = 768
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    var errBody = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini Embedding ({Model}) returned {StatusCode}: {Body}", modelName, response.StatusCode, errBody);
                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("embedding", out var embeddingElem) &&
                    embeddingElem.TryGetProperty("values", out var valuesElem) &&
                    valuesElem.ValueKind == JsonValueKind.Array)
                {
                    var vector = new float[valuesElem.GetArrayLength()];
                    var idx = 0;
                    foreach (var val in valuesElem.EnumerateArray())
                    {
                        vector[idx++] = val.GetSingle();
                    }
                    return vector;
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error calling Gemini Embedding model {Model}", modelName);
            }
        }

        return null;
    }

    public async Task<string> GenerateChatResponseAsync(string systemInstruction, string userPrompt, List<ChatHistoryItemDto>? history = null)
    {
        var apiKey = GetApiKey();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return "ขณะนี้ยังไม่ได้ตั้งค่า GEMINI_API_KEY ในระบบ กรุณาตั้งค่าในไฟล์ .env เพื่อเปิดใช้งานผู้ช่วย AI ครับ";
        }

        var contents = new List<object>();
        if (history != null)
        {
            foreach (var h in history.TakeLast(8))
            {
                if (string.IsNullOrWhiteSpace(h.Text)) continue;
                var role = h.Role?.ToLowerInvariant() == "model" ? "model" : "user";
                contents.Add(new
                {
                    role,
                    parts = new[] { new { text = h.Text } }
                });
            }
        }

        contents.Add(new
        {
            role = "user",
            parts = new[] { new { text = userPrompt } }
        });

        var modelsToTry = new[] { "gemini-2.5-flash", "gemini-2.0-flash", "gemini-1.5-flash" };
        string lastError = string.Empty;

        foreach (var modelName in modelsToTry)
        {
            try
            {
                var url = $"https://generativelanguage.googleapis.com/v1beta/models/{modelName}:generateContent?key={apiKey}";
                var payload = new
                {
                    system_instruction = new
                    {
                        parts = new[] { new { text = systemInstruction } }
                    },
                    contents,
                    generationConfig = new
                    {
                        temperature = 0.25,
                        maxOutputTokens = 1500
                    }
                };

                var json = JsonSerializer.Serialize(payload);
                using var content = new StringContent(json, Encoding.UTF8, "application/json");
                using var response = await _httpClient.PostAsync(url, content);

                if (!response.IsSuccessStatusCode)
                {
                    lastError = await response.Content.ReadAsStringAsync();
                    _logger.LogWarning("Gemini Chat ({Model}) returned {StatusCode}: {Body}", modelName, response.StatusCode, lastError);
                    continue;
                }

                var responseBody = await response.Content.ReadAsStringAsync();
                using var doc = JsonDocument.Parse(responseBody);
                if (doc.RootElement.TryGetProperty("candidates", out var candidates) &&
                    candidates.ValueKind == JsonValueKind.Array &&
                    candidates.GetArrayLength() > 0)
                {
                    var firstCandidate = candidates[0];
                    if (firstCandidate.TryGetProperty("content", out var contentElem) &&
                        contentElem.TryGetProperty("parts", out var partsElem) &&
                        partsElem.ValueKind == JsonValueKind.Array &&
                        partsElem.GetArrayLength() > 0)
                    {
                        var textPart = partsElem[0];
                        if (textPart.TryGetProperty("text", out var textElem))
                        {
                            return textElem.GetString() ?? string.Empty;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                lastError = ex.Message;
                _logger.LogWarning(ex, "Error calling Gemini Chat model {Model}", modelName);
            }
        }

        _logger.LogError("All Gemini chat models failed. Last error: {Error}", lastError);
        return "ขออภัยครับ ระบบ AI ไม่สามารถเชื่อมต่อกับเซิร์ฟเวอร์ Gemini ได้ในขณะนี้ (กรุณาตรวจสอบค่า GEMINI_API_KEY ใน .env ว่าเริ่มต้นด้วย AIzaSy... จาก Google AI Studio หรือไม่ครับ)";
    }

    public double ComputeCosineSimilarity(float[]? vectorA, float[]? vectorB)
    {
        if (vectorA == null || vectorB == null || vectorA.Length == 0 || vectorB.Length == 0)
            return 0.0;

        int len = Math.Min(vectorA.Length, vectorB.Length);
        double dot = 0.0;
        double magA = 0.0;
        double magB = 0.0;

        for (int i = 0; i < len; i++)
        {
            double a = vectorA[i];
            double b = vectorB[i];
            dot += a * b;
            magA += a * a;
            magB += b * b;
        }

        double denom = Math.Sqrt(magA) * Math.Sqrt(magB);
        if (denom <= 1e-12) return 0.0;
        return dot / denom;
    }

    private static readonly HashSet<string> IgnoredSpecKeys = new(StringComparer.OrdinalIgnoreCase)
    {
        "__v", "tags", "brand", "brands", "image", "status", "createdAt", "updatedAt",
        "category", "description", "modelName", "isFeatured", "soldCount", "viewCount"
    };

    /// <summary>
    /// Recursively flattens schema-less JSONB specifications (including nested objects and arrays)
    /// into readable key-value pairs for vector embedding and RAG prompts.
    /// </summary>
    public string ExtractSpecificationsText(JsonDocument? specifications)
    {
        if (specifications == null) return string.Empty;

        try
        {
            var root = specifications.RootElement;
            if (root.ValueKind != JsonValueKind.Object) return string.Empty;

            var specTarget = root.TryGetProperty("specifications", out var nested) && nested.ValueKind == JsonValueKind.Object
                ? nested
                : root;

            var specPairs = new List<string>();
            FlattenJsonElement(specTarget, prefix: string.Empty, specPairs);
            return string.Join(", ", specPairs);
        }
        catch
        {
            return string.Empty;
        }
    }

    private static void FlattenJsonElement(JsonElement element, string prefix, List<string> output)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (var prop in element.EnumerateObject())
                {
                    if (prop.Name.StartsWith("_") || IgnoredSpecKeys.Contains(prop.Name))
                        continue;

                    var nextPrefix = string.IsNullOrEmpty(prefix) ? prop.Name : $"{prefix}.{prop.Name}";
                    FlattenJsonElement(prop.Value, nextPrefix, output);
                }
                break;

            case JsonValueKind.Array:
                var primitiveItems = new List<string>();
                foreach (var item in element.EnumerateArray())
                {
                    if (item.ValueKind == JsonValueKind.Object || item.ValueKind == JsonValueKind.Array)
                    {
                        FlattenJsonElement(item, prefix, output);
                    }
                    else
                    {
                        var valStr = item.ToString().Trim();
                        if (!string.IsNullOrWhiteSpace(valStr))
                            primitiveItems.Add(valStr);
                    }
                }
                if (primitiveItems.Count > 0 && !string.IsNullOrEmpty(prefix))
                    output.Add($"{prefix}: {string.Join(" / ", primitiveItems)}");
                break;

            default:
                var scalar = element.ToString().Trim();
                if (!string.IsNullOrWhiteSpace(scalar) && !string.IsNullOrEmpty(prefix))
                    output.Add($"{prefix}: {scalar}");
                break;
        }
    }

    public string BuildProductEmbeddingText(Product product, string? categoryName = null)
    {
        var sb = new StringBuilder();
        var cat = categoryName ?? product.Category?.Name ?? "IT Hardware";
        var tags = product.Tags != null && product.Tags.Length > 0 ? string.Join(", ", product.Tags) : "";

        sb.Append($"[PRODUCT / สินค้า] SKU: {product.Sku} | Name: {product.Name} | Brand: {product.Brand} | Model: {product.ModelName} | Category: {cat} | Price: {product.Price:N2} THB");
        if (!string.IsNullOrWhiteSpace(tags))
            sb.Append($" | Tags: {tags}");
        if (!string.IsNullOrWhiteSpace(product.Description))
            sb.Append($" | Description: {product.Description}");

        var specText = ExtractSpecificationsText(product.Specifications);
        if (!string.IsNullOrWhiteSpace(specText))
            sb.Append($" | Specifications: {specText}");

        return sb.ToString();
    }

    public string BuildSalesOrderEmbeddingText(SalesOrder so)
    {
        var customerName = so.Customer?.CompanyName ?? "Walk-in / Online Customer";
        var customerType = so.Customer?.CustomerType ?? "B2C";
        var itemsText = so.SalesOrderItems != null && so.SalesOrderItems.Any()
            ? string.Join(", ", so.SalesOrderItems.Select(i => $"{i.Product?.Name ?? i.ProductId.ToString()} x{i.Quantity} @{i.UnitPrice:N2} THB"))
            : "No items";

        return $"[SALES_ORDER / REVENUE / บิลขาย / รายได้] OrderNumber: {so.OrderNumber} | Date: {so.OrderDate:yyyy-MM-dd} | Customer: {customerName} ({customerType}) | Channel: {so.OrderChannel} | OrderStatus: {so.Status} | PaymentStatus: {so.PaymentStatus} | Courier: {so.Courier ?? "-"} | TrackingNumber: {so.TrackingNumber ?? "-"} | TotalRevenue: {so.TotalAmount:N2} THB | Items Sold: [{itemsText}]";
    }

    public string BuildPurchaseOrderEmbeddingText(PurchaseOrder po)
    {
        var supplierName = po.Supplier?.CompanyName ?? "Unknown Supplier";
        var contact = po.Supplier?.ContactName ?? "-";
        var phone = po.Supplier?.Phone ?? "-";
        var itemsText = po.PurchaseOrderItems != null && po.PurchaseOrderItems.Any()
            ? string.Join(", ", po.PurchaseOrderItems.Select(i => $"{i.Product?.Name ?? i.ProductId.ToString()} x{i.Quantity} @Cost {i.UnitCost:N2} THB"))
            : "No items";

        return $"[PURCHASE_ORDER / EXPENSE / ใบสั่งซื้อ / รายจ่าย] PoNumber: {po.PoNumber} | Date: {po.OrderDate:yyyy-MM-dd} | Supplier: {supplierName} (Contact: {contact}, Phone: {phone}) | Ref: {po.ReferenceNumber ?? "-"} | PaymentTerms: {po.PaymentTerms ?? "-"} | ExpectedDelivery: {(po.ExpectedDeliveryDate.HasValue ? po.ExpectedDeliveryDate.Value.ToString("yyyy-MM-dd") : "-")} | Status: {po.Status} | Remarks: {po.Remarks ?? "-"} | SubTotal: {po.SubTotal:N2} THB | VAT ({po.VatRate}%): {po.TaxAmount:N2} THB | TotalExpense: {po.TotalAmount:N2} THB | Items Purchased: [{itemsText}]";
    }

    public string BuildSupplierEmbeddingText(Supplier supplier, IEnumerable<string>? suppliedProductNames = null)
    {
        var poCount = supplier.PurchaseOrders?.Count ?? 0;
        var productsList = suppliedProductNames != null && suppliedProductNames.Any()
            ? string.Join(", ", suppliedProductNames.Distinct().Take(20))
            : "-";

        return $"[SUPPLIER / คู่ค้าซัพพลายเออร์] Company: {supplier.CompanyName} | ContactPerson: {supplier.ContactName ?? "-"} | Phone: {supplier.Phone ?? "-"} | Email: {supplier.Email ?? "-"} | TaxId: {supplier.TaxId ?? "-"} | PaymentTerms: {supplier.PaymentTerms ?? "-"} | Address: {supplier.Address ?? "-"} | TotalPOs: {poCount} | Products Supplied: [{productsList}]";
    }

    public string BuildCustomerEmbeddingText(Customer customer, int orderCount = 0, decimal totalSpent = 0)
    {
        return $"[CUSTOMER / ลูกค้า] Name: {customer.CompanyName} | Type: {customer.CustomerType} | Phone: {customer.Phone ?? "-"} | Email: {customer.Email ?? "-"} | TaxId: {customer.TaxId ?? "-"} | CreditTermDays: {customer.CreditTermDays} | CreditLimit: {customer.CreditLimit:N2} THB | Address: {customer.Address ?? "-"} | TotalOrders: {orderCount} | TotalSpent: {totalSpent:N2} THB";
    }

    public async Task<bool> TryEmbedProductAsync(AppDbContext context, Guid productId)
    {
        try
        {
            if (!IsConfigured) return false;
            var product = await context.Products
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == productId);
            if (product == null) return false;

            var text = BuildProductEmbeddingText(product, product.Category?.Name);
            var vec = await GenerateEmbeddingAsync(text);
            if (vec == null) return false;

            product.Embedding = vec;
            product.EmbeddingUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fault-tolerant fallback: Failed to auto-embed Product {ProductId}", productId);
            return false;
        }
    }

    public async Task<bool> TryEmbedSalesOrderAsync(AppDbContext context, Guid salesOrderId)
    {
        try
        {
            if (!IsConfigured) return false;
            var so = await context.SalesOrders
                .Include(s => s.Customer)
                .Include(s => s.SalesOrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == salesOrderId);
            if (so == null) return false;

            var text = BuildSalesOrderEmbeddingText(so);
            var vec = await GenerateEmbeddingAsync(text);
            if (vec == null) return false;

            so.Embedding = vec;
            so.EmbeddingUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fault-tolerant fallback: Failed to auto-embed SalesOrder {SalesOrderId}", salesOrderId);
            return false;
        }
    }

    public async Task<bool> TryEmbedPurchaseOrderAsync(AppDbContext context, Guid purchaseOrderId)
    {
        try
        {
            if (!IsConfigured) return false;
            var po = await context.PurchaseOrders
                .Include(p => p.Supplier)
                .Include(p => p.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(p => p.Id == purchaseOrderId);
            if (po == null) return false;

            var text = BuildPurchaseOrderEmbeddingText(po);
            var vec = await GenerateEmbeddingAsync(text);
            if (vec == null) return false;

            po.Embedding = vec;
            po.EmbeddingUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fault-tolerant fallback: Failed to auto-embed PurchaseOrder {PurchaseOrderId}", purchaseOrderId);
            return false;
        }
    }

    public async Task<bool> TryEmbedSupplierAsync(AppDbContext context, Guid supplierId)
    {
        try
        {
            if (!IsConfigured) return false;
            var supplier = await context.Suppliers
                .Include(s => s.PurchaseOrders)
                .ThenInclude(po => po.PurchaseOrderItems)
                .ThenInclude(i => i.Product)
                .FirstOrDefaultAsync(s => s.Id == supplierId);
            if (supplier == null) return false;

            var suppliedNames = supplier.PurchaseOrders
                .SelectMany(po => po.PurchaseOrderItems)
                .Where(i => i.Product != null)
                .Select(i => i.Product.Name)
                .Distinct();

            var text = BuildSupplierEmbeddingText(supplier, suppliedNames);
            var vec = await GenerateEmbeddingAsync(text);
            if (vec == null) return false;

            supplier.Embedding = vec;
            supplier.EmbeddingUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fault-tolerant fallback: Failed to auto-embed Supplier {SupplierId}", supplierId);
            return false;
        }
    }

    public async Task<bool> TryEmbedCustomerAsync(AppDbContext context, Guid customerId)
    {
        try
        {
            if (!IsConfigured) return false;
            var customer = await context.Customers
                .Include(c => c.SalesOrders)
                .FirstOrDefaultAsync(c => c.Id == customerId);
            if (customer == null) return false;

            var validOrders = customer.SalesOrders.Where(so => so.Status != "Cancelled").ToList();
            var text = BuildCustomerEmbeddingText(customer, validOrders.Count, validOrders.Sum(so => so.TotalAmount));
            var vec = await GenerateEmbeddingAsync(text);
            if (vec == null) return false;

            customer.Embedding = vec;
            customer.EmbeddingUpdatedAt = DateTime.UtcNow;
            await context.SaveChangesAsync();
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Fault-tolerant fallback: Failed to auto-embed Customer {CustomerId}", customerId);
            return false;
        }
    }
}
