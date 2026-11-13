using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using JamineERP.Backend.Data;
using JamineERP.Backend.DTOs;
using JamineERP.Backend.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class AiController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IGeminiAiService _aiService;

    public AiController(AppDbContext context, IGeminiAiService aiService)
    {
        _context = context;
        _aiService = aiService;
    }

    // 1. GET /api/ai/status
    [HttpGet("status")]
    [Authorize(Roles = "Admin,Sales,Purchasing,Warehouse")]
    public async Task<IActionResult> GetStatus()
    {
        var status = new AiStatusDto
        {
            ApiKeyConfigured = _aiService.IsConfigured,
            Products = new AiModuleCountDto
            {
                Total = await _context.Products.CountAsync(),
                Embedded = await _context.Products.CountAsync(p => p.Embedding != null)
            },
            SalesOrders = new AiModuleCountDto
            {
                Total = await _context.SalesOrders.CountAsync(),
                Embedded = await _context.SalesOrders.CountAsync(s => s.Embedding != null)
            },
            PurchaseOrders = new AiModuleCountDto
            {
                Total = await _context.PurchaseOrders.CountAsync(),
                Embedded = await _context.PurchaseOrders.CountAsync(p => p.Embedding != null)
            },
            Suppliers = new AiModuleCountDto
            {
                Total = await _context.Suppliers.CountAsync(),
                Embedded = await _context.Suppliers.CountAsync(s => s.Embedding != null)
            },
            Customers = new AiModuleCountDto
            {
                Total = await _context.Customers.CountAsync(),
                Embedded = await _context.Customers.CountAsync(c => c.Embedding != null)
            }
        };

        return Ok(status);
    }

    // 2. POST /api/ai/sync-embeddings
    [HttpPost("sync-embeddings")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> SyncEmbeddings([FromBody] SyncEmbeddingsRequestDto? dto)
    {
        if (!_aiService.IsConfigured)
        {
            return BadRequest(new { message = "ยังไม่ได้ตั้งค่า GEMINI_API_KEY ในระบบ กรุณาตรวจสอบไฟล์ .env" });
        }

        var force = dto?.ForceRebuildAll ?? false;
        var module = (dto?.Module ?? "all").ToLowerInvariant();
        var result = new SyncEmbeddingsResponseDto();

        // 1) Products
        if (module == "all" || module == "products")
        {
            var productIds = await _context.Products
                .Where(p => force || p.Embedding == null)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var id in productIds)
            {
                if (await _aiService.TryEmbedProductAsync(_context, id))
                    result.ProductsSynced++;
                else
                    result.FailedCount++;
            }
        }

        // 2) SalesOrders (Revenue)
        if (module == "all" || module == "sales-orders")
        {
            var soIds = await _context.SalesOrders
                .Where(s => force || s.Embedding == null)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var id in soIds)
            {
                if (await _aiService.TryEmbedSalesOrderAsync(_context, id))
                    result.SalesOrdersSynced++;
                else
                    result.FailedCount++;
            }
        }

        // 3) PurchaseOrders (Expense)
        if (module == "all" || module == "purchase-orders")
        {
            var poIds = await _context.PurchaseOrders
                .Where(p => force || p.Embedding == null)
                .Select(p => p.Id)
                .ToListAsync();

            foreach (var id in poIds)
            {
                if (await _aiService.TryEmbedPurchaseOrderAsync(_context, id))
                    result.PurchaseOrdersSynced++;
                else
                    result.FailedCount++;
            }
        }

        // 4) Suppliers
        if (module == "all" || module == "suppliers")
        {
            var supplierIds = await _context.Suppliers
                .Where(s => force || s.Embedding == null)
                .Select(s => s.Id)
                .ToListAsync();

            foreach (var id in supplierIds)
            {
                if (await _aiService.TryEmbedSupplierAsync(_context, id))
                    result.SuppliersSynced++;
                else
                    result.FailedCount++;
            }
        }

        // 5) Customers
        if (module == "all" || module == "customers")
        {
            var customerIds = await _context.Customers
                .Where(c => force || c.Embedding == null)
                .Select(c => c.Id)
                .ToListAsync();

            foreach (var id in customerIds)
            {
                if (await _aiService.TryEmbedCustomerAsync(_context, id))
                    result.CustomersSynced++;
                else
                    result.FailedCount++;
            }
        }

        result.Message = $"ซิงค์ข้อมูล Vector สำเร็จรวม {result.TotalSynced} รายการ (Products: {result.ProductsSynced}, SO: {result.SalesOrdersSynced}, PO: {result.PurchaseOrdersSynced}, Suppliers: {result.SuppliersSynced}, Customers: {result.CustomersSynced})";
        return Ok(result);
    }

    // 3. POST /api/ai/semantic-search
    [HttpPost("semantic-search")]
    [AllowAnonymous]
    public async Task<IActionResult> SemanticSearch([FromBody] SemanticSearchRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Query))
            return Ok(new List<SemanticProductMatchDto>());

        var queryVector = await _aiService.GenerateEmbeddingAsync(dto.Query);
        var queryLower = dto.Query.Trim().ToLowerInvariant();

        var productsQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == "ACTIVE");

        if (!string.IsNullOrWhiteSpace(dto.CategoryName) && dto.CategoryName != "All")
        {
            productsQuery = productsQuery.Where(p => p.Category.Name == dto.CategoryName);
        }

        if (dto.OnlyInStock)
        {
            productsQuery = productsQuery.Where(p => (p.OnHandQuantity - p.ReservedQuantity) > 0);
        }

        var products = await productsQuery.ToListAsync();

        var scored = products.Select(p =>
        {
            double sim = queryVector != null && p.Embedding != null
                ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding)
                : 0.0;

            // Hybrid keyword boost for exact SKU/Brand/Model/Name matches
            if (p.Name.ToLowerInvariant().Contains(queryLower) ||
                p.Brand.ToLowerInvariant().Contains(queryLower) ||
                p.ModelName.ToLowerInvariant().Contains(queryLower) ||
                p.Sku.ToLowerInvariant().Contains(queryLower))
            {
                sim = Math.Min(0.99, Math.Max(sim + 0.12, 0.78));
            }

            return MapToSemanticMatch(p, sim, includeCost: false);
        })
        .Where(x => x.SimilarityScore >= 0.45)
        .OrderByDescending(x => x.SimilarityScore)
        .Take(dto.TopK > 0 ? dto.TopK : 12)
        .ToList();

        return Ok(scored);
    }

    private static SemanticProductMatchDto MapToSemanticMatch(Models.Product p, double score, bool includeCost)
    {
        JsonElement? specsClone = null;
        if (p.Specifications != null)
        {
            try
            {
                specsClone = JsonDocument.Parse(p.Specifications.RootElement.GetRawText()).RootElement;
            }
            catch
            {
                specsClone = null;
            }
        }

        return new SemanticProductMatchDto
        {
            Id = p.Id,
            Sku = p.Sku,
            Name = p.Name,
            Brand = p.Brand,
            ModelName = p.ModelName,
            Description = p.Description,
            Price = p.Price,
            Cost = includeCost ? p.Cost : 0,
            OnHandQuantity = p.OnHandQuantity,
            ReservedQuantity = p.ReservedQuantity,
            AvailableQuantity = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity),
            CategoryId = p.CategoryId,
            CategoryName = p.Category?.Name ?? "",
            ImageUrl = p.Image?.Url,
            Tags = p.Tags ?? Array.Empty<string>(),
            Specifications = specsClone,
            SimilarityScore = Math.Round(score, 4)
        };
    }

    // 4. POST /api/ai/check-duplicate
    [HttpPost("check-duplicate")]
    [Authorize(Roles = "Admin,Purchasing")]
    public async Task<IActionResult> CheckDuplicateProduct([FromBody] DuplicateCheckRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Name) && string.IsNullOrWhiteSpace(dto.Sku))
            return Ok(new DuplicateCheckResponseDto { HasDuplicates = false });

        var candidateText = $"[PRODUCT / สินค้า] SKU: {dto.Sku ?? ""} | Name: {dto.Name} | Brand: {dto.Brand ?? ""} | Model: {dto.ModelName ?? ""}";
        var queryVector = await _aiService.GenerateEmbeddingAsync(candidateText);
        var nameLower = (dto.Name ?? "").Trim().ToLowerInvariant();
        var skuLower = (dto.Sku ?? "").Trim().ToLowerInvariant();

        var existingProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync();

        var matches = new List<SemanticProductMatchDto>();
        foreach (var p in existingProducts)
        {
            double sim = queryVector != null && p.Embedding != null
                ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding)
                : 0.0;

            if (!string.IsNullOrWhiteSpace(skuLower) && p.Sku.ToLowerInvariant() == skuLower)
            {
                sim = 1.0;
            }
            else if (!string.IsNullOrWhiteSpace(nameLower) && p.Name.ToLowerInvariant() == nameLower)
            {
                sim = Math.Max(sim, 0.98);
            }

            if (sim >= dto.Threshold)
            {
                matches.Add(MapToSemanticMatch(p, sim, includeCost: true));
            }
        }

        var sorted = matches.OrderByDescending(m => m.SimilarityScore).Take(5).ToList();
        return Ok(new DuplicateCheckResponseDto
        {
            HasDuplicates = sorted.Count > 0,
            Matches = sorted
        });
    }

    // 5. POST /api/ai/storefront-chat (User Storefront AI Assistant)
    [HttpPost("storefront-chat")]
    [AllowAnonymous]
    public async Task<IActionResult> StorefrontChat([FromBody] ChatRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "กรุณาพิมพ์ข้อความคำถาม" });

        var queryVector = await _aiService.GenerateEmbeddingAsync(dto.Message);
        var msgLower = dto.Message.Trim().ToLowerInvariant();

        var activeProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => p.Status == "ACTIVE")
            .ToListAsync();

        var rankedProducts = activeProducts
            .Select(p =>
            {
                double sim = queryVector != null && p.Embedding != null
                    ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding)
                    : 0.0;

                if (p.Name.ToLowerInvariant().Contains(msgLower) ||
                    p.Brand.ToLowerInvariant().Contains(msgLower) ||
                    (p.Category?.Name?.ToLowerInvariant().Contains(msgLower) ?? false))
                {
                    sim = Math.Max(sim + 0.15, 0.75);
                }

                return new { Product = p, Score = sim };
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.IsFeatured)
            .Take(6)
            .ToList();

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine("=== รายการสินค้าในร้าน JAMiNE IT ที่เกี่ยวข้องกับคำถามลูกค้า (Real-Time Stock & Price) ===");
        foreach (var item in rankedProducts)
        {
            var p = item.Product;
            var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
            var specsText = _aiService.ExtractSpecificationsText(p.Specifications);
            contextBuilder.AppendLine($"- สินค้า: {p.Name} | แบรนด์: {p.Brand} | หมวดหมู่: {p.Category?.Name} | ราคาขาย: {p.Price:N2} บาท | สถานะสต็อกพร้อมส่งทันที: {(avail > 0 ? $"มีของพร้อมส่ง {avail} ชิ้น" : "สินค้าหมดชั่วคราว")} | สเปค: {(string.IsNullOrWhiteSpace(specsText) ? "-" : specsText)} | รายละเอียด: {p.Description}");
        }

        var systemInstruction = @"คุณคือ 'JAMiNE AI Assistant' ผู้เชี่ยวชาญแนะนำอุปกรณ์คอมพิวเตอร์และไอทีประจำหน้าร้าน Jamine IT
กฎเหล็ก:
1. ตอบคำถามลูกค้าด้วยภาษาไทยที่สุภาพ เป็นกันเอง กระชับ อ่านง่าย (ใช้ Bullet points)
2. อ้างอิงชื่อสินค้า ราคาขาย สเปคสินค้า และจำนวนสต็อกพร้อมส่งจากข้อมูลที่ระบบส่งให้เท่านั้น ห้ามแต่งชื่อสินค้าหรือราคาขึ้นเองเด็ดขาด
3. ห้ามเปิดเผยข้อมูลต้นทุน (Cost) หรือข้อมูลภายในบริษัทโดยเด็ดขาด
4. หากลูกค้าถามหาสเปกหรือมีงบประมาณ ให้ช่วยคัดเลือกตัวที่คุ้มค่าและมีสต็อกพร้อมส่งให้ทันที";

        var userPrompt = $"{contextBuilder}\n\nคำถามจากลูกค้า: {dto.Message}";
        var reply = await _aiService.GenerateChatResponseAsync(systemInstruction, userPrompt, dto.History);

        var suggestions = rankedProducts
            .Where(x => x.Score >= 0.45 || rankedProducts.IndexOf(x) < 3)
            .Take(4)
            .Select(x => MapToSemanticMatch(x.Product, x.Score, includeCost: false))
            .ToList();

        return Ok(new ChatResponseDto
        {
            Reply = reply,
            SuggestedProducts = suggestions,
            Timestamp = DateTime.UtcNow
        });
    }

    // 6. POST /api/ai/admin-copilot (Admin ERP Copilot across all 6 modules)
    [HttpPost("admin-copilot")]
    [Authorize(Roles = "Admin,Sales,Purchasing,Warehouse")]
    public async Task<IActionResult> AdminCopilot([FromBody] ChatRequestDto dto)
    {
        if (string.IsNullOrWhiteSpace(dto.Message))
            return BadRequest(new { message = "กรุณาพิมพ์คำถามสำหรับ ERP Copilot" });

        var queryVector = await _aiService.GenerateEmbeddingAsync(dto.Message);
        var msgLower = dto.Message.Trim().ToLowerInvariant();
        var referencedDocs = new List<ChatDocumentRefDto>();

        // --- A. Real-Time Global ERP Summary ---
        var allSalesOrders = await _context.SalesOrders
            .AsNoTracking()
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
            .ThenInclude(i => i.Product)
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync();

        var allPurchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
            .ThenInclude(i => i.Product)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

        var allProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .ToListAsync();

        var allSuppliers = await _context.Suppliers
            .AsNoTracking()
            .ToListAsync();

        var allCustomers = await _context.Customers
            .AsNoTracking()
            .ToListAsync();

        var recentGRs = await _context.GoodsReceipts
            .AsNoTracking()
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.ReceivedByUser)
            .OrderByDescending(gr => gr.ReceiptDate)
            .Take(8)
            .ToListAsync();

        var recentGIs = await _context.GoodsIssues
            .AsNoTracking()
            .Include(gi => gi.SalesOrder)
            .Include(gi => gi.IssuedByUser)
            .OrderByDescending(gi => gi.IssueDate)
            .Take(8)
            .ToListAsync();

        var validSOs = allSalesOrders.Where(s => s.Status != "Cancelled").ToList();
        var validPOs = allPurchaseOrders.Where(p => p.Status != "Cancelled").ToList();

        var totalRevenue = validSOs.Sum(s => s.TotalAmount);
        var totalExpense = validPOs.Sum(p => p.TotalAmount);
        var pendingSlips = allSalesOrders.Count(s => s.PaymentStatus == "Checking");
        var pendingInboundPOs = allPurchaseOrders.Where(p => p.Status == "Pending" || p.Status == "Approved").ToList();
        var pendingOutboundSOs = allSalesOrders.Where(s => s.Status == "Pending" && s.PaymentStatus == "Paid").ToList();

        // --- B. Vector Ranking across 5 Modules ---
        var topProducts = allProducts
            .Select(p => new
            {
                Item = p,
                Score = (queryVector != null && p.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding) : 0.0)
                        + (p.Name.ToLowerInvariant().Contains(msgLower) || p.Sku.ToLowerInvariant().Contains(msgLower) ? 0.25 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .Take(5)
            .ToList();

        var topSOs = allSalesOrders
            .Select(so => new
            {
                Item = so,
                Score = (queryVector != null && so.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, so.Embedding) : 0.0)
                        + (so.OrderNumber.ToLowerInvariant().Contains(msgLower) || (so.Customer?.CompanyName?.ToLowerInvariant().Contains(msgLower) ?? false) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.OrderDate)
            .Take(6)
            .ToList();

        var topPOs = allPurchaseOrders
            .Select(po => new
            {
                Item = po,
                Score = (queryVector != null && po.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, po.Embedding) : 0.0)
                        + (po.PoNumber.ToLowerInvariant().Contains(msgLower) || (po.Supplier?.CompanyName?.ToLowerInvariant().Contains(msgLower) ?? false) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.OrderDate)
            .Take(6)
            .ToList();

        var topSuppliers = allSuppliers
            .Select(s => new
            {
                Item = s,
                Score = (queryVector != null && s.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, s.Embedding) : 0.0)
                        + (s.CompanyName.ToLowerInvariant().Contains(msgLower) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .Take(4)
            .ToList();

        var topCustomers = allCustomers
            .Select(c => new
            {
                Item = c,
                Score = (queryVector != null && c.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, c.Embedding) : 0.0)
                        + (c.CompanyName.ToLowerInvariant().Contains(msgLower) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .Take(4)
            .ToList();

        // Populate clickable document references
        foreach (var so in topSOs.Take(3))
        {
            referencedDocs.Add(new ChatDocumentRefDto
            {
                Type = "SO",
                Id = so.Item.Id,
                Code = so.Item.OrderNumber,
                Label = $"{so.Item.OrderNumber} ({so.Item.Customer?.CompanyName ?? "ลูกค้า"} - ฿{so.Item.TotalAmount:N2})",
                Route = $"/admin/sales-orders/{so.Item.Id}",
                SimilarityScore = Math.Round(so.Score, 3)
            });
        }

        foreach (var po in topPOs.Take(3))
        {
            referencedDocs.Add(new ChatDocumentRefDto
            {
                Type = "PO",
                Id = po.Item.Id,
                Code = po.Item.PoNumber,
                Label = $"{po.Item.PoNumber} ({po.Item.Supplier?.CompanyName ?? "ซัพพลายเออร์"} - ฿{po.Item.TotalAmount:N2})",
                Route = $"/admin/purchase-orders/{po.Item.Id}",
                SimilarityScore = Math.Round(po.Score, 3)
            });
        }

        // Build comprehensive RAG Context for Gemini
        var ctx = new StringBuilder();
        ctx.AppendLine("=== 1. ภาพรวมการเงินและงานค้างทั้งระบบ (Real-Time ERP Snapshot) ===");
        ctx.AppendLine($"- รายได้รวมสะสมทั้งหมด (Sales Orders ที่ไม่ยกเลิก {validSOs.Count} บิล): {totalRevenue:N2} บาท");
        ctx.AppendLine($"- รายจ่ายสั่งซื้อรวมสะสมทั้งหมด (Purchase Orders ที่ไม่ยกเลิก {validPOs.Count} บิล): {totalExpense:N2} บาท");
        ctx.AppendLine($"- ส่วนต่างกระแสเงินสดสุทธิ (Revenue - Expense): {(totalRevenue - totalExpense):N2} บาท");
        ctx.AppendLine($"- สลิปโอนเงินรอตรวจสอบ (PaymentStatus = Checking): {pendingSlips} รายการ");
        ctx.AppendLine($"- งานค้างคลังขาเข้า (Inbound POs รอรับของเข้าคลัง): {pendingInboundPOs.Count} ใบ ({string.Join(", ", pendingInboundPOs.Take(5).Select(p => $"{p.PoNumber} จาก {p.Supplier?.CompanyName} ยอด {p.TotalAmount:N2} บาท"))})");
        ctx.AppendLine($"- งานค้างคลังขาออก (Outbound SOs จ่ายเงินแล้วรอแพ็คจัดส่ง): {pendingOutboundSOs.Count} ใบ ({string.Join(", ", pendingOutboundSOs.Take(5).Select(s => $"{s.OrderNumber} ลูกค้า {s.Customer?.CompanyName} ยอด {s.TotalAmount:N2} บาท"))})");

        ctx.AppendLine("\n=== 2. บิลขาย / รายได้แต่ละครั้ง (Sales Orders - Revenue) ที่เกี่ยวข้องสูงสุด ===");
        foreach (var x in topSOs)
        {
            var so = x.Item;
            var itemsStr = string.Join(", ", so.SalesOrderItems.Select(i => $"{i.Product?.Name} x{i.Quantity} (@{i.UnitPrice:N2})"));
            ctx.AppendLine($"- เลขบิลขาย: {so.OrderNumber} | วันที่: {so.OrderDate:yyyy-MM-dd HH:mm} | ลูกค้า: {so.Customer?.CompanyName} | ช่องทาง: {so.OrderChannel} | สถานะบิล: {so.Status} | การชำระเงิน: {so.PaymentStatus} | ยอดรายได้: {so.TotalAmount:N2} บาท | สินค้าในบิล: [{itemsStr}]");
        }

        ctx.AppendLine("\n=== 3. ใบสั่งซื้อ / รายจ่ายแต่ละครั้ง (Purchase Orders - Expense) ที่เกี่ยวข้องสูงสุด ===");
        foreach (var x in topPOs)
        {
            var po = x.Item;
            var itemsStr = string.Join(", ", po.PurchaseOrderItems.Select(i => $"{i.Product?.Name} x{i.Quantity} (@ทุน {i.UnitCost:N2})"));
            ctx.AppendLine($"- เลขใบสั่งซื้อ: {po.PoNumber} | วันที่: {po.OrderDate:yyyy-MM-dd} | ซัพพลายเออร์: {po.Supplier?.CompanyName} | เครดิต: {po.PaymentTerms ?? "-"} | หมายเหตุ: {po.Remarks ?? "-"} | สถานะ: {po.Status} | ยอดรายจ่ายรวมภาษี: {po.TotalAmount:N2} บาท | สินค้าที่สั่งซื้อ: [{itemsStr}]");
        }

        ctx.AppendLine("\n=== 4. ข้อมูลสินค้า สต็อก และราคาทุน-ราคาขาย (Products & Inventory) ===");
        foreach (var x in topProducts)
        {
            var p = x.Item;
            var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
            var specsText = _aiService.ExtractSpecificationsText(p.Specifications);
            ctx.AppendLine($"- SKU: {p.Sku} | ชื่อ: {p.Name} | ราคาทุน: {p.Cost:N2} บาท | ราคาขาย: {p.Price:N2} บาท | กำไรต่อชิ้น: {(p.Price - p.Cost):N2} บาท | สต็อกจริง (OnHand): {p.OnHandQuantity} | จองแล้ว (Reserved): {p.ReservedQuantity} | พร้อมขาย (Available): {avail} | ขายไปแล้ว: {p.SoldCount} ชิ้น | สเปค: {(string.IsNullOrWhiteSpace(specsText) ? "-" : specsText)}");
        }

        var lowStockList = allProducts.Where(p => p.OnHandQuantity <= 5).OrderBy(p => p.OnHandQuantity).Take(6).ToList();
        if (lowStockList.Any())
        {
            ctx.AppendLine($"- สินค้าสต็อกต่ำกว่าเกณฑ์ (<= 5 ชิ้น): {string.Join(", ", lowStockList.Select(p => $"{p.Name} (SKU: {p.Sku}, เหลือ {p.OnHandQuantity} ชิ้น)"))}");
        }

        ctx.AppendLine("\n=== 5. ข้อมูลคู่ค้าซัพพลายเออร์ (Suppliers) และลูกค้า (Customers) ===");
        foreach (var s in topSuppliers)
        {
            ctx.AppendLine($"- ซัพพลายเออร์: {s.Item.CompanyName} | ผู้ติดต่อ: {s.Item.ContactName ?? "-"} | โทร: {s.Item.Phone ?? "-"} | เครดิต: {s.Item.PaymentTerms ?? "-"} | TaxID: {s.Item.TaxId ?? "-"}");
        }
        foreach (var c in topCustomers)
        {
            var custOrders = validSOs.Where(so => so.CustomerId == c.Item.Id).ToList();
            ctx.AppendLine($"- ลูกค้า: {c.Item.CompanyName} ({c.Item.CustomerType}) | โทร: {c.Item.Phone ?? "-"} | เครดิต: {c.Item.CreditTermDays} วัน (วงเงิน {c.Item.CreditLimit:N2} บาท) | ซื้อสะสม: {custOrders.Count} บิล รวม {custOrders.Sum(o => o.TotalAmount):N2} บาท");
        }

        ctx.AppendLine("\n=== 6. ประวัติการรับของเข้าคลัง (GR) และเบิกส่งของ (GI) ล่าสุด ===");
        foreach (var gr in recentGRs.Take(4))
        {
            ctx.AppendLine($"- ใบรับของเข้า GR: {gr.GrNumber} | อ้างอิง PO: {gr.PurchaseOrder?.PoNumber} | วันที่รับ: {gr.ReceiptDate:yyyy-MM-dd} | ผู้รับ: {gr.ReceivedByUser?.Username} | หมายเหตุ: {gr.Remarks}");
        }
        foreach (var gi in recentGIs.Take(4))
        {
            ctx.AppendLine($"- ใบเบิกส่งของ GI: {gi.GiNumber} | อ้างอิง SO: {gi.SalesOrder?.OrderNumber} | วันที่ส่ง: {gi.IssueDate:yyyy-MM-dd} | ผู้เบิก: {gi.IssuedByUser?.Username} | หมายเหตุ: {gi.Remarks}");
        }

        var systemInstruction = @"คุณคือ 'Jamine ERP AI Copilot' ผู้ช่วยวิเคราะห์ข้อมูลธุรกิจอัจฉริยะระดับผู้บริหารและแอดมินของระบบ Jamine ERP
หน้าที่ของคุณ:
1. ตอบคำถามเกี่ยวกับรายได้ (Sales Orders / SO), รายจ่ายแต่ละครั้ง (Purchase Orders / PO), สินค้า/สต็อก/กำไร, ซัพพลายเออร์, ลูกค้า และสถานะคลังสินค้า (Inbound GR / Outbound GI) อย่างแม่นยำ
2. อ้างอิงเลขที่เอกสารจริงเสมอ (เช่น SO2026..., PO2026..., GR..., GI..., SKU) พร้อมระบุวันที่และตัวเลขจำนวนเงินให้ชัดเจน
3. จัดรูปแบบคำตอบให้อ่านง่ายด้วยหัวข้อและ Bullet points กระชับ ชัดเจน และเป็นมืออาชีพ";

        var userPrompt = $"{ctx}\n\nคำถามจากผู้ดูแลระบบ: {dto.Message}";
        var reply = await _aiService.GenerateChatResponseAsync(systemInstruction, userPrompt, dto.History);

        return Ok(new ChatResponseDto
        {
            Reply = reply,
            ReferencedDocuments = referencedDocs,
            Timestamp = DateTime.UtcNow
        });
    }

    // 7. POST /api/ai/dashboard-brief (Module 6: AI Executive Business Brief)
    [HttpPost("dashboard-brief")]
    [Authorize(Roles = "Admin")]
    public async Task<IActionResult> GenerateDashboardBrief()
    {
        var validSOs = await _context.SalesOrders
            .AsNoTracking()
            .Where(s => s.Status != "Cancelled")
            .ToListAsync();

        var validPOs = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Product)
            .Where(p => p.Status != "Cancelled")
            .ToListAsync();

        var totalRevenue = validSOs.Sum(s => s.TotalAmount);
        var totalExpense = validPOs.Sum(p => p.TotalAmount);
        var pendingSlips = validSOs.Count(s => s.PaymentStatus == "Checking");
        var pendingInboundPOs = validPOs.Count(p => p.Status == "Pending" || p.Status == "Approved");
        var pendingOutboundSOs = validSOs.Count(s => s.Status == "Pending" && s.PaymentStatus == "Paid");

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => p.OnHandQuantity <= 5)
            .OrderBy(p => p.OnHandQuantity)
            .Take(6)
            .ToListAsync();

        var lowStockDetails = new List<string>();
        foreach (var prod in lowStockProducts)
        {
            var lastSupplier = validPOs
                .Where(po => po.PurchaseOrderItems.Any(i => i.ProductId == prod.Id))
                .OrderByDescending(po => po.OrderDate)
                .Select(po => po.Supplier?.CompanyName)
                .FirstOrDefault() ?? "ยังไม่มีประวัติซัพพลายเออร์";

            lowStockDetails.Add($"{prod.Name} (SKU: {prod.Sku}, คงเหลือ {prod.OnHandQuantity} ชิ้น, ซัพพลายเออร์แนะนำ: {lastSupplier})");
        }

        var prompt = $@"วิเคราะห์ข้อมูลสถานะธุรกิจของ Jamine ERP ณ ปัจจุบัน:
- รายได้รวม (Sales Orders): {totalRevenue:N2} บาท (จาก {validSOs.Count} บิล)
- รายจ่ายสั่งซื้อรวม (Purchase Orders): {totalExpense:N2} บาท (จาก {validPOs.Count} บิล)
- กระแสเงินสดสุทธิ (Revenue - Expense): {(totalRevenue - totalExpense):N2} บาท
- สลิปโอนเงินรออนุมัติด่วน: {pendingSlips} รายการ
- ใบสั่งซื้อ PO รอรับของเข้าคลัง (Inbound Pending): {pendingInboundPOs} รายการ
- บิลขาย SO จ่ายเงินแล้วรอแพ็คจัดส่ง (Outbound Pending): {pendingOutboundSOs} รายการ
- สินค้าสต็อกต่ำกว่าเกณฑ์ (<= 5 ชิ้น): {(lowStockDetails.Any() ? string.Join(" | ", lowStockDetails) : "ไม่มีสินค้าใกล้หมด")}

กรุณาสรุปเป็น JSON Object เท่านั้น (ห้ามใส่ ```json หรือข้อความอื่นนอกเหนือจาก JSON) ตามโครงสร้างนี้:
{{
  ""financialSummary"": ""สรุปสถานะรายได้ รายจ่าย และสภาพคล่องสั้นๆ 1-2 ประโยค"",
  ""urgentActions"": ""สรุปงานด่วนที่แอดมิน/คลังสินค้าต้องรีบจัดการทันที 1-2 ประโยค"",
  ""restockRecommendations"": ""แนะนำรายการสินค้าที่ควรเปิดใบ PO สั่งซื้อเพิ่มพร้อมระบุชื่อซัพพลายเออร์ 1-2 ประโยค"",
  ""executiveTakeaway"": ""ข้อเสนอแนะเชิงกลยุทธ์สำหรับผู้บริหาร 1 ประโยค""
}}";

        var systemInstruction = "คุณคือที่ปรึกษาผู้บริหาร ERP ตอบกลับเป็น JSON ที่ถูกต้องตามคีย์ที่กำหนดเท่านั้น";
        var rawResponse = await _aiService.GenerateChatResponseAsync(systemInstruction, prompt);

        var brief = new DashboardAiBriefDto
        {
            TotalRevenue = totalRevenue,
            TotalExpense = totalExpense,
            PendingInboundPOs = pendingInboundPOs,
            PendingOutboundSOs = pendingOutboundSOs,
            PendingSlipVerifications = pendingSlips,
            GeneratedAt = DateTime.UtcNow
        };

        try
        {
            var cleaned = rawResponse.Trim();
            if (cleaned.StartsWith("```"))
            {
                var firstNewline = cleaned.IndexOf('\n');
                if (firstNewline >= 0) cleaned = cleaned.Substring(firstNewline + 1);
                if (cleaned.EndsWith("```")) cleaned = cleaned.Substring(0, cleaned.Length - 3);
                cleaned = cleaned.Trim();
            }

            using var doc = JsonDocument.Parse(cleaned);
            var root = doc.RootElement;
            brief.FinancialSummary = root.TryGetProperty("financialSummary", out var fs) ? fs.GetString() ?? "" : "";
            brief.UrgentActions = root.TryGetProperty("urgentActions", out var ua) ? ua.GetString() ?? "" : "";
            brief.RestockRecommendations = root.TryGetProperty("restockRecommendations", out var rr) ? rr.GetString() ?? "" : "";
            brief.ExecutiveTakeaway = root.TryGetProperty("executiveTakeaway", out var et) ? et.GetString() ?? "" : "";
        }
        catch
        {
            brief.FinancialSummary = $"รายได้รวม ฿{totalRevenue:N2} เทียบกับรายจ่ายใบสั่งซื้อ ฿{totalExpense:N2} (สุทธิ ฿{(totalRevenue - totalExpense):N2})";
            brief.UrgentActions = $"มีสลิปรอตรวจสอบ {pendingSlips} รายการ, รอรับของเข้าคลัง {pendingInboundPOs} PO และรอแพ็คจัดส่ง {pendingOutboundSOs} SO";
            brief.RestockRecommendations = lowStockDetails.Any()
                ? $"สินค้าใกล้หมดที่ควรสั่งเพิ่ม: {string.Join(", ", lowStockDetails.Take(3))}"
                : "สต็อกสินค้าทุกรายการอยู่ในเกณฑ์ปกติ";
            brief.ExecutiveTakeaway = rawResponse;
        }

        return Ok(brief);
    }
}
