using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Text.RegularExpressions;
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
        var tokens = ExpandQueryTokens(dto.Query);
        var (minBudget, maxBudget) = ExtractBudgetRange(dto.Query);

        var productsQuery = _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.Status != null && p.Status.ToLower() == "active");

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

            double tokenBoost = ComputeProductTokenMatchScore(p, tokens, _aiService.ExtractSpecificationsText(p.Specifications));
            if (tokenBoost > 0)
            {
                sim = Math.Min(0.99, Math.Max(sim + tokenBoost * 0.25, 0.72 + Math.Min(0.25, tokenBoost * 0.1)));
            }

            if (IsWithinBudget(p.Price, minBudget, maxBudget))
            {
                sim = Math.Min(0.99, sim + 0.08);
            }

            return MapToSemanticMatch(p, sim, includeCost: false);
        })
        .Where(x => x.SimilarityScore >= 0.45)
        .OrderByDescending(x => x.SimilarityScore)
        .Take(dto.TopK > 0 ? dto.TopK : 12)
        .ToList();

        return Ok(scored);
    }

    private static readonly Dictionary<string, string[]> ThaiHardwareSynonyms = new(StringComparer.OrdinalIgnoreCase)
    {
        ["เมาส์"] = new[] { "mouse", "gaming mouse" },
        ["เม้าส์"] = new[] { "mouse", "gaming mouse" },
        ["คีย์บอร์ด"] = new[] { "keyboard", "mechanical keyboard" },
        ["แป้นพิมพ์"] = new[] { "keyboard" },
        ["จอ"] = new[] { "monitor", "display" },
        ["มอนิเตอร์"] = new[] { "monitor", "display" },
        ["หน้าจอ"] = new[] { "monitor", "display" },
        ["โน้ตบุ๊ก"] = new[] { "notebook", "laptop" },
        ["โน๊ตบุ๊ค"] = new[] { "notebook", "laptop" },
        ["โน้ตบุ๊ค"] = new[] { "notebook", "laptop" },
        ["แล็ปท็อป"] = new[] { "notebook", "laptop" },
        ["การ์ดจอ"] = new[] { "graphics card", "gpu", "vga", "rtx", "gtx", "radeon" },
        ["ซีพียู"] = new[] { "cpu", "processor", "ryzen", "intel", "core" },
        ["แรม"] = new[] { "ram", "memory", "ddr4", "ddr5" },
        ["เอสเอสดี"] = new[] { "ssd", "nvme", "storage" },
        ["ฮาร์ดดิสก์"] = new[] { "hdd", "ssd", "storage" },
        ["หูฟัง"] = new[] { "headset", "headphone", "earphone" },
        ["ไมโครโฟน"] = new[] { "microphone", "mic" },
        ["เมนบอร์ด"] = new[] { "motherboard", "mainboard" },
        ["เพาเวอร์ซัพพลาย"] = new[] { "power supply", "psu" },
        ["พาวเวอร์ซัพพลาย"] = new[] { "power supply", "psu" },
        ["เคส"] = new[] { "case", "chassis" },
        ["พัดลม"] = new[] { "cooler", "fan", "aio" },
        ["ชุดระบายความร้อน"] = new[] { "cooler", "liquid", "aio" },
        ["เล่นเกม"] = new[] { "gaming", "rtx", "hz", "rgb", "geforce" },
        ["เกมมิ่ง"] = new[] { "gaming", "rtx", "hz", "rgb" },
        ["กราฟิก"] = new[] { "rtx", "graphics", "quadro", "ips", "oled", "16gb", "32gb" },
        ["ตัดต่อ"] = new[] { "rtx", "16gb", "32gb", "ssd", "ips" },
        ["ทำงาน"] = new[] { "office", "business", "laptop", "notebook", "monitor" }
    };

    private static List<string> ExpandQueryTokens(string query)
    {
        var lower = (query ?? string.Empty).Trim().ToLowerInvariant();
        var tokens = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (Match m in Regex.Matches(lower, @"[a-z0-9\-_]+"))
        {
            if (m.Value.Length >= 2)
                tokens.Add(m.Value);
        }

        foreach (var kv in ThaiHardwareSynonyms)
        {
            if (lower.Contains(kv.Key))
            {
                foreach (var mapped in kv.Value)
                    tokens.Add(mapped);
            }
        }

        return tokens.ToList();
    }

    private static (decimal? Min, decimal? Max) ExtractBudgetRange(string query)
    {
        if (string.IsNullOrWhiteSpace(query)) return (null, null);
        var normalized = query.Replace(",", "");
        var numbers = new List<decimal>();

        foreach (Match m in Regex.Matches(normalized, @"\b\d{3,7}\b"))
        {
            if (decimal.TryParse(m.Value, NumberStyles.Any, CultureInfo.InvariantCulture, out var val) && val >= 100)
                numbers.Add(val);
        }

        if (numbers.Count >= 2)
        {
            var min = Math.Min(numbers[0], numbers[1]);
            var max = Math.Max(numbers[0], numbers[1]);
            return (min, max);
        }
        if (numbers.Count == 1)
        {
            var single = numbers[0];
            if (normalized.Contains("ไม่เกิน") || normalized.Contains("ต่ำกว่า") || normalized.Contains("under"))
                return (0, single);
            return (single * 0.7m, single * 1.25m);
        }
        return (null, null);
    }

    private static bool IsWithinBudget(decimal price, decimal? minBudget, decimal? maxBudget)
    {
        if (!minBudget.HasValue && !maxBudget.HasValue) return false;
        if (minBudget.HasValue && price < minBudget.Value) return false;
        if (maxBudget.HasValue && price > maxBudget.Value) return false;
        return true;
    }

    private static readonly HashSet<string> EnglishStopWords = new(StringComparer.OrdinalIgnoreCase)
    {
        "do", "you", "have", "any", "the", "for", "is", "are", "can", "what", "with",
        "and", "or", "want", "need", "looking", "buy", "sell", "price", "under", "over",
        "around", "in", "of", "to", "my", "me", "please", "some", "good", "best", "recommend"
    };

    private static double ComputeProductTokenMatchScore(Models.Product p, List<string> tokens, string specsText)
    {
        if (tokens.Count == 0) return 0.0;
        var meaningfulTokens = tokens.Where(t => !EnglishStopWords.Contains(t)).ToList();
        if (meaningfulTokens.Count == 0) return 0.0;

        var tagsText = p.Tags != null && p.Tags.Length > 0 ? string.Join(" ", p.Tags) : string.Empty;
        var haystack = $"{p.Sku} {p.Name} {p.Brand} {p.ModelName} {p.Category?.Name} {tagsText} {p.Description} {specsText}".ToLowerInvariant();
        int matched = 0;
        foreach (var rawToken in meaningfulTokens)
        {
            var token = rawToken.ToLowerInvariant();
            var singular = token == "mice" ? "mouse" : (token.EndsWith("s") && token.Length > 3 ? token[..^1] : token);
            if (haystack.Contains(token) || (singular != token && haystack.Contains(singular)))
                matched++;
        }
        return (double)matched / Math.Min(meaningfulTokens.Count, 3);
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
        var tokens = ExpandQueryTokens(dto.Message);
        var (minBudget, maxBudget) = ExtractBudgetRange(dto.Message);
        var hasBudgetQuery = minBudget.HasValue || maxBudget.HasValue;

        var activeProducts = await _context.Products
            .AsNoTracking()
            .Include(p => p.Category)
            .Where(p => !p.IsDeleted && p.Status != null && p.Status.ToLower() == "active")
            .ToListAsync();

        var categorySummary = activeProducts
            .GroupBy(p => p.Category?.Name ?? "อื่นๆ")
            .Select(g => $"{g.Key} ({g.Count()} รุ่น)")
            .ToList();

        var scoredProducts = activeProducts
            .Select(p =>
            {
                var specsText = _aiService.ExtractSpecificationsText(p.Specifications);
                double vectorSim = queryVector != null && p.Embedding != null
                    ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding)
                    : 0.0;

                double tokenScore = ComputeProductTokenMatchScore(p, tokens, specsText);
                bool inBudget = IsWithinBudget(p.Price, minBudget, maxBudget);

                double finalScore = vectorSim;
                if (tokenScore > 0)
                {
                    finalScore = Math.Min(0.99, Math.Max(finalScore + tokenScore * 0.25, 0.72 + Math.Min(0.25, tokenScore * 0.1)));
                }
                if (inBudget)
                {
                    finalScore = Math.Min(0.99, Math.Max(finalScore + 0.15, 0.65));
                }

                var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
                return new
                {
                    Product = p,
                    SpecsText = specsText,
                    Score = finalScore,
                    IsRelevant = tokenScore > 0 || inBudget || vectorSim >= 0.56,
                    InStock = avail > 0
                };
            })
            .ToList();

        var isBestSellerQuery = dto.Message.Contains("ขายดี") || dto.Message.Contains("แนะนำ") && !hasBudgetQuery && tokens.Count == 0;
        var matchedProducts = scoredProducts
            .Where(x => x.IsRelevant || isBestSellerQuery)
            .OrderByDescending(x => x.InStock)
            .ThenByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.SoldCount)
            .Take(8)
            .ToList();

        var contextBuilder = new StringBuilder();
        contextBuilder.AppendLine($"=== หมวดหมู่สินค้าที่มีจำหน่ายจริงในร้าน Jamine IT: {string.Join(", ", categorySummary)} ===");
        if (hasBudgetQuery)
        {
            contextBuilder.AppendLine($"=== งบประมาณที่ลูกค้าสนใจ: {(minBudget ?? 0):N0} - {(maxBudget ?? 9999999):N0} บาท ===");
        }

        if (matchedProducts.Count > 0)
        {
            contextBuilder.AppendLine("=== รายการสินค้าจริงในร้านที่ตรงกับคำถามลูกค้ามากที่สุด (Real-Time Stock & Price) ===");
            foreach (var item in matchedProducts)
            {
                var p = item.Product;
                var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
                contextBuilder.AppendLine($"- สินค้า: {p.Name} | แบรนด์: {p.Brand} | หมวดหมู่: {p.Category?.Name} | ราคาขาย: {p.Price:N2} บาท | สถานะสต็อก: {(avail > 0 ? $"พร้อมส่ง {avail} ชิ้น" : "สินค้าหมดชั่วคราว")} | สเปค: {(string.IsNullOrWhiteSpace(item.SpecsText) ? "-" : item.SpecsText)} | รายละเอียด: {p.Description}");
            }
        }

        // แนบรายการสินค้าในร้านเพิ่มเติม (Catalog Overview) เพื่อให้ AI ค้นหาข้ามภาษาไทย-อังกฤษได้เองโดยไม่ต้องพึ่ง Dictionary ตายตัว
        var matchedIds = matchedProducts.Select(m => m.Product.Id).ToHashSet();
        var remainingCatalog = scoredProducts
            .Where(x => !matchedIds.Contains(x.Product.Id))
            .OrderByDescending(x => x.InStock)
            .ThenByDescending(x => x.Score)
            .ThenByDescending(x => x.Product.SoldCount)
            .Take(100)
            .ToList();

        if (remainingCatalog.Count > 0)
        {
            contextBuilder.AppendLine("=== รายการสินค้าทั้งหมดเพิ่มเติมในร้าน Jamine IT (สำหรับตรวจสอบว่ามีขายหรือไม่และแนะนำสินค้าใกล้เคียง) ===");
            foreach (var item in remainingCatalog)
            {
                var p = item.Product;
                var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
                contextBuilder.AppendLine($"- {p.Name} | หมวด: {p.Category?.Name} | แบรนด์: {p.Brand} | ราคา: {p.Price:N2} บาท | สต็อก: {(avail > 0 ? $"พร้อมส่ง {avail} ชิ้น" : "หมดชั่วคราว")} | สเปค: {(string.IsNullOrWhiteSpace(item.SpecsText) ? "-" : item.SpecsText)}");
            }
        }
        else if (matchedProducts.Count == 0)
        {
            contextBuilder.AppendLine("=== ผลการค้นหาในฐานข้อมูล: [ไม่พบสินค้าในร้านขณะนี้] ===");
        }

        var systemInstruction = @"คุณคือ 'JAMiNE AI Assistant' ผู้เชี่ยวชาญแนะนำอุปกรณ์คอมพิวเตอร์และไอทีประจำหน้าร้าน Jamine IT
กฎเหล็ก:
1. รองรับทั้งภาษาไทยและภาษาอังกฤษ: ให้ตอบกลับเป็นภาษาเดียวกับที่ลูกค้าถามมา (หากลูกค้าถามภาษาไทย ตอบภาษาไทยอย่างสุภาพ เป็นกันเอง; หากลูกค้าถามภาษาอังกฤษ ตอบภาษาอังกฤษอย่างสุภาพและเป็นมืออาชีพ)
2. รูปแบบการตอบ: กระชับ อ่านง่าย ใช้เครื่องหมายขีด (-) แทน bullet และหลีกเลี่ยงการใช้ดอกจัน (**)
3. เน้นสินค้าจริงของร้านก่อนเสมอ: ตรวจสอบทั้งรายการที่ตรงกับคำถามและรายการสินค้าทั้งหมดของร้านที่ระบบส่งให้ หากมีสินค้าที่ตรงหรือเกี่ยวข้อง (รวมถึงคำถามภาษาอังกฤษ/ภาษาไทยหรือคำเรียกต่างกัน) ให้แสดงชื่อรุ่น ราคาขาย สเปคเด่น และสถานะสต็อก (พร้อมส่งกี่ชิ้น หรือหมดชั่วคราว) ทันทีในคำตอบแรก ห้ามถามย้อนลูกค้าโดยไม่แสดงรายการสินค้า
4. กรณีไม่มีสินค้าที่ถาม: หากตรวจสอบรายการสินค้าและหมวดหมู่ทั้งหมดที่ระบบส่งให้แล้วพบว่าไม่มีสินค้าประเภทหรือรุ่นนั้นจำหน่ายจริง ให้แจ้งอย่างสุภาพและชัดเจนว่ายังไม่มีสินค้าดังกล่าวจำหน่าย (ภาษาไทย: 'ขออภัยครับ ขณะนี้ทางร้าน Jamine IT ยังไม่มีสินค้าประเภท/รุ่นดังกล่าวจำหน่าย' | ภาษาอังกฤษ: 'Sorry, Jamine IT currently does not carry that product or category.') พร้อมแนะนำหมวดหมู่สินค้าที่มีจำหน่ายจริงในร้านให้ลูกค้าเลือกดูแทน ห้ามแนะนำสินค้าคนละหมวดหรือแต่งชื่อรุ่น/ราคาเองเด็ดขาด
5. ห้ามเปิดเผยข้อมูลต้นทุน (Cost) หรือข้อมูลภายในบริษัทโดยเด็ดขาด";

        var userPrompt = $"{contextBuilder}\n\nคำถามจากลูกค้า: {dto.Message}";
        var reply = await _aiService.GenerateChatResponseAsync(systemInstruction, userPrompt, dto.History);

        var normalizedReply = Regex.Replace(reply ?? string.Empty, @"\s+", " ");
        var mentionedInReply = !string.IsNullOrWhiteSpace(normalizedReply)
            ? scoredProducts
                .Where(x => !string.IsNullOrWhiteSpace(x.Product.Name))
                .Select(x => new
                {
                    Item = x,
                    Index = normalizedReply.IndexOf(Regex.Replace(x.Product.Name, @"\s+", " "), StringComparison.OrdinalIgnoreCase)
                })
                .Where(x => x.Index >= 0)
                .OrderBy(x => x.Index)
                .Take(4)
                .Select(x => MapToSemanticMatch(x.Item.Product, Math.Max(x.Item.Score, 0.8), includeCost: false))
                .ToList()
            : new List<SemanticProductMatchDto>();

        var suggestions = mentionedInReply.Count > 0
            ? mentionedInReply
            : matchedProducts
                .Take(4)
                .Select(x => MapToSemanticMatch(x.Product, x.Score, includeCost: false))
                .ToList();

        return Ok(new ChatResponseDto
        {
            Reply = reply ?? string.Empty,
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
            .Where(so => !so.IsDeleted)
            .Include(so => so.Customer)
            .Include(so => so.SalesOrderItems)
            .ThenInclude(i => i.Product)
            .OrderByDescending(so => so.OrderDate)
            .ToListAsync();

        var allPurchaseOrders = await _context.PurchaseOrders
            .AsNoTracking()
            .Where(po => !po.IsDeleted)
            .Include(po => po.Supplier)
            .Include(po => po.PurchaseOrderItems)
            .ThenInclude(i => i.Product)
            .OrderByDescending(po => po.OrderDate)
            .ToListAsync();

        var allQuotations = await _context.Quotations
            .AsNoTracking()
            .Where(q => !q.IsDeleted)
            .Include(q => q.Customer)
            .OrderByDescending(q => q.QuoteDate)
            .Take(15)
            .ToListAsync();

        var allProducts = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted)
            .Include(p => p.Category)
            .ToListAsync();

        var allSuppliers = await _context.Suppliers
            .AsNoTracking()
            .Where(s => !s.IsDeleted)
            .ToListAsync();

        var allCustomers = await _context.Customers
            .AsNoTracking()
            .Where(c => !c.IsDeleted)
            .ToListAsync();

        var recentGRs = await _context.GoodsReceipts
            .AsNoTracking()
            .Where(gr => !gr.IsDeleted)
            .Include(gr => gr.PurchaseOrder)
            .Include(gr => gr.ReceivedByUser)
            .OrderByDescending(gr => gr.ReceiptDate)
            .Take(8)
            .ToListAsync();

        var recentGIs = await _context.GoodsIssues
            .AsNoTracking()
            .Where(gi => !gi.IsDeleted)
            .Include(gi => gi.SalesOrder)
            .Include(gi => gi.IssuedByUser)
            .OrderByDescending(gi => gi.IssueDate)
            .Take(8)
            .ToListAsync();

        var validSOs = allSalesOrders.Where(s => !string.Equals(s.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();
        var validPOs = allPurchaseOrders.Where(p => !string.Equals(p.Status, "Cancelled", StringComparison.OrdinalIgnoreCase)).ToList();

        var totalRevenue = validSOs.Sum(s => s.TotalAmount);
        var totalExpense = validPOs.Sum(p => p.TotalAmount);
        var pendingSlips = validSOs.Count(s => string.Equals(s.PaymentStatus, "Checking", StringComparison.OrdinalIgnoreCase));
        var pendingInboundPOs = validPOs.Where(p => string.Equals(p.Status, "Pending", StringComparison.OrdinalIgnoreCase) || string.Equals(p.Status, "Approved", StringComparison.OrdinalIgnoreCase)).ToList();
        var pendingOutboundSOs = validSOs.Where(s => string.Equals(s.Status, "Pending", StringComparison.OrdinalIgnoreCase) && string.Equals(s.PaymentStatus, "Paid", StringComparison.OrdinalIgnoreCase)).ToList();
        var inventoryCostValue = allProducts.Sum(p => p.Cost * Math.Max(0, p.OnHandQuantity));
        var inventoryRetailValue = allProducts.Sum(p => p.Price * Math.Max(0, p.OnHandQuantity));
        var channelBreakdown = validSOs
            .GroupBy(s => string.IsNullOrWhiteSpace(s.OrderChannel) ? "Unknown" : s.OrderChannel)
            .Select(g => $"{g.Key}: {g.Count()} บิล (฿{g.Sum(x => x.TotalAmount):N2})")
            .ToList();

        // --- B. Vector Ranking across 5 Modules ---
        var topProducts = allProducts
            .Select(p => new
            {
                Item = p,
                Score = (queryVector != null && p.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, p.Embedding) : 0.0)
                        + (p.Name.ToLowerInvariant().Contains(msgLower) || p.Sku.ToLowerInvariant().Contains(msgLower) ? 0.25 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .ThenByDescending(x => x.Item.SoldCount)
            .Take(50)
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
            .Take(15)
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
            .Take(15)
            .ToList();

        var topSuppliers = allSuppliers
            .Select(s => new
            {
                Item = s,
                Score = (queryVector != null && s.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, s.Embedding) : 0.0)
                        + (s.CompanyName.ToLowerInvariant().Contains(msgLower) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .Take(15)
            .ToList();

        var topCustomers = allCustomers
            .Select(c => new
            {
                Item = c,
                Score = (queryVector != null && c.Embedding != null ? _aiService.ComputeCosineSimilarity(queryVector, c.Embedding) : 0.0)
                        + (c.CompanyName.ToLowerInvariant().Contains(msgLower) ? 0.3 : 0.0)
            })
            .OrderByDescending(x => x.Score)
            .Take(15)
            .ToList();

        // Build comprehensive RAG Context for Gemini
        var ctx = new StringBuilder();
        ctx.AppendLine("=== 1. ภาพรวมการเงินและงานค้างทั้งระบบ (Real-Time ERP Snapshot) ===");
        ctx.AppendLine($"- รายได้รวมสะสมทั้งหมด (Sales Orders ที่ไม่ยกเลิก {validSOs.Count} บิล): {totalRevenue:N2} บาท");
        ctx.AppendLine($"- รายจ่ายสั่งซื้อรวมสะสมทั้งหมด (Purchase Orders ที่ไม่ยกเลิก {validPOs.Count} บิล): {totalExpense:N2} บาท");
        ctx.AppendLine($"- ส่วนต่างกระแสเงินสดสุทธิ (Revenue - Expense): {(totalRevenue - totalExpense):N2} บาท");
        ctx.AppendLine($"- มูลค่าสต็อกคงเหลือปัจจุบัน: ราคาทุน {inventoryCostValue:N2} บาท | ราคาขายปลีก {inventoryRetailValue:N2} บาท");
        ctx.AppendLine($"- ยอดขายแยกตามช่องทาง: {(channelBreakdown.Any() ? string.Join(" | ", channelBreakdown) : "ยังไม่มีข้อมูล")}");
        ctx.AppendLine($"- สลิปโอนเงินรอตรวจสอบ (PaymentStatus = Checking): {pendingSlips} รายการ");
        ctx.AppendLine($"- งานค้างคลังขาเข้า (Inbound POs รอรับของเข้าคลัง): {pendingInboundPOs.Count} ใบ ({string.Join(", ", pendingInboundPOs.Take(5).Select(p => $"{p.PoNumber} จาก {p.Supplier?.CompanyName} ยอด {p.TotalAmount:N2} บาท"))})");
        ctx.AppendLine($"- งานค้างคลังขาออก (Outbound SOs จ่ายเงินแล้วรอแพ็คจัดส่ง): {pendingOutboundSOs.Count} ใบ ({string.Join(", ", pendingOutboundSOs.Take(5).Select(s => $"{s.OrderNumber} ลูกค้า {s.Customer?.CompanyName} ยอด {s.TotalAmount:N2} บาท"))})");

        ctx.AppendLine("\n=== 2. บิลขาย / รายได้แต่ละครั้ง (Sales Orders - Revenue) ===");
        if (topSOs.Count == 0) ctx.AppendLine("- ยังไม่มีบิลขาย (Sales Orders) ในระบบ");
        foreach (var x in topSOs)
        {
            var so = x.Item;
            var itemsStr = string.Join(", ", so.SalesOrderItems.Select(i => $"{i.Product?.Name} x{i.Quantity} (@{i.UnitPrice:N2})"));
            ctx.AppendLine($"- เลขบิลขาย: {so.OrderNumber} | วันที่: {so.OrderDate:yyyy-MM-dd HH:mm} | ลูกค้า: {so.Customer?.CompanyName} | ช่องทาง: {so.OrderChannel} | สถานะบิล: {so.Status} | การชำระเงิน: {so.PaymentStatus} | ยอดรายได้: {so.TotalAmount:N2} บาท | สินค้าในบิล: [{itemsStr}]");
        }

        ctx.AppendLine("\n=== 3. ใบสั่งซื้อ / รายจ่ายแต่ละครั้ง (Purchase Orders - Expense) ===");
        if (topPOs.Count == 0) ctx.AppendLine("- ยังไม่มีใบสั่งซื้อ (Purchase Orders) ในระบบ");
        foreach (var x in topPOs)
        {
            var po = x.Item;
            var itemsStr = string.Join(", ", po.PurchaseOrderItems.Select(i => $"{i.Product?.Name} x{i.Quantity} (@ทุน {i.UnitCost:N2})"));
            ctx.AppendLine($"- เลขใบสั่งซื้อ: {po.PoNumber} | วันที่: {po.OrderDate:yyyy-MM-dd} | ซัพพลายเออร์: {po.Supplier?.CompanyName} | เครดิต: {po.PaymentTerms ?? "-"} | หมายเหตุ: {po.Remarks ?? "-"} | สถานะ: {po.Status} | ยอดรายจ่ายรวมภาษี: {po.TotalAmount:N2} บาท | สินค้าที่สั่งซื้อ: [{itemsStr}]");
        }

        ctx.AppendLine("\n=== 4. ใบเสนอราคา (Quotations ล่าสุด) ===");
        if (allQuotations.Count == 0) ctx.AppendLine("- ยังไม่มีใบเสนอราคาในระบบ");
        foreach (var q in allQuotations)
        {
            ctx.AppendLine($"- เลขใบเสนอราคา: {q.QuoteNumber} | วันที่: {q.QuoteDate:yyyy-MM-dd} | ลูกค้า: {q.Customer?.CompanyName ?? "-"} | สถานะ: {q.Status} | ยอดรวม: {q.TotalAmount:N2} บาท | ใช้ได้ถึง: {q.ValidUntil:yyyy-MM-dd}");
        }

        ctx.AppendLine("\n=== 5. ข้อมูลสินค้า สต็อก และราคาทุน-ราคาขาย (Products & Inventory) ===");
        foreach (var x in topProducts)
        {
            var p = x.Item;
            var avail = Math.Max(0, p.OnHandQuantity - p.ReservedQuantity);
            var specsText = _aiService.ExtractSpecificationsText(p.Specifications);
            ctx.AppendLine($"- SKU: {p.Sku} | ชื่อ: {p.Name} | หมวด: {p.Category?.Name} | ราคาทุน: {p.Cost:N2} บาท | ราคาขาย: {p.Price:N2} บาท | กำไรต่อชิ้น: {(p.Price - p.Cost):N2} บาท | สต็อกจริง (OnHand): {p.OnHandQuantity} | จองแล้ว (Reserved): {p.ReservedQuantity} | พร้อมขาย (Available): {avail} | ขายไปแล้ว: {p.SoldCount} ชิ้น | สเปค: {(string.IsNullOrWhiteSpace(specsText) ? "-" : specsText)}");
        }

        var lowStockList = allProducts.Where(p => p.OnHandQuantity <= 5).OrderBy(p => p.OnHandQuantity).Take(10).ToList();
        if (lowStockList.Any())
        {
            ctx.AppendLine($"- สินค้าสต็อกต่ำกว่าเกณฑ์ (<= 5 ชิ้น): {string.Join(", ", lowStockList.Select(p => $"{p.Name} (SKU: {p.Sku}, เหลือ {p.OnHandQuantity} ชิ้น)"))}");
        }

        ctx.AppendLine("\n=== 6. ข้อมูลคู่ค้าซัพพลายเออร์ (Suppliers) และลูกค้า (Customers) ===");
        foreach (var s in topSuppliers)
        {
            ctx.AppendLine($"- ซัพพลายเออร์: {s.Item.CompanyName} | ผู้ติดต่อ: {s.Item.ContactName ?? "-"} | โทร: {s.Item.Phone ?? "-"} | เครดิต: {s.Item.PaymentTerms ?? "-"} | TaxID: {s.Item.TaxId ?? "-"}");
        }
        foreach (var c in topCustomers)
        {
            var custOrders = validSOs.Where(so => so.CustomerId == c.Item.Id).ToList();
            ctx.AppendLine($"- ลูกค้า: {c.Item.CompanyName} ({c.Item.CustomerType}) | โทร: {c.Item.Phone ?? "-"} | เครดิต: {c.Item.CreditTermDays} วัน (วงเงิน {c.Item.CreditLimit:N2} บาท) | ซื้อสะสม: {custOrders.Count} บิล รวม {custOrders.Sum(o => o.TotalAmount):N2} บาท");
        }

        ctx.AppendLine("\n=== 7. ประวัติการรับของเข้าคลัง (GR) และเบิกส่งของ (GI) ล่าสุด ===");
        if (recentGRs.Count == 0) ctx.AppendLine("- ยังไม่มีใบรับของเข้าคลัง (GR)");
        foreach (var gr in recentGRs.Take(6))
        {
            ctx.AppendLine($"- ใบรับของเข้า GR: {gr.GrNumber} | อ้างอิง PO: {gr.PurchaseOrder?.PoNumber} | วันที่รับ: {gr.ReceiptDate:yyyy-MM-dd} | ผู้รับ: {gr.ReceivedByUser?.Username} | หมายเหตุ: {gr.Remarks}");
        }
        if (recentGIs.Count == 0) ctx.AppendLine("- ยังไม่มีใบเบิกส่งของ (GI)");
        foreach (var gi in recentGIs.Take(6))
        {
            ctx.AppendLine($"- ใบเบิกส่งของ GI: {gi.GiNumber} | อ้างอิง SO: {gi.SalesOrder?.OrderNumber} | วันที่ส่ง: {gi.IssueDate:yyyy-MM-dd} | ผู้เบิก: {gi.IssuedByUser?.Username} | หมายเหตุ: {gi.Remarks}");
        }

        var systemInstruction = @"คุณคือ 'Jamine ERP AI Copilot' ผู้ช่วยวิเคราะห์ข้อมูลธุรกิจอัจฉริยะระดับผู้บริหารและแอดมินของระบบ Jamine ERP
หน้าที่และกฎเหล็ก:
1. รองรับทั้งภาษาไทยและภาษาอังกฤษ: ตอบกลับเป็นภาษาเดียวกับคำถามของผู้ดูแลระบบ
2. ตอบคำถามเกี่ยวกับรายได้ (Sales Orders / SO), รายจ่าย (Purchase Orders / PO), ใบเสนอราคา (Quotations), สินค้า/สต็อก/กำไร, ซัพพลายเออร์, ลูกค้า และสถานะคลังสินค้า (Inbound GR / Outbound GI) อย่างแม่นยำจากข้อมูลจริงที่ระบบส่งให้เท่านั้น ห้ามแต่งตัวเลขหรือเลขที่เอกสารเองเด็ดขาด
3. อ้างอิงเลขที่เอกสารจริงเสมอ (เช่น SO..., PO..., QT..., GR..., GI..., SKU) พร้อมระบุวันที่และตัวเลขจำนวนเงินให้ชัดเจน หากหมวดใดยังไม่มีข้อมูลในระบบให้ระบุตรงๆ ว่ายังไม่มีรายการ
4. จัดรูปแบบคำตอบให้อ่านง่าย ใช้ขีด (-) แทน bullet และหลีกเลี่ยงการใช้ดอกจัน (**)";

        var userPrompt = $"{ctx}\n\nคำถามจากผู้ดูแลระบบ: {dto.Message}";
        var reply = await _aiService.GenerateChatResponseAsync(systemInstruction, userPrompt, dto.History) ?? string.Empty;

        // Populate clickable document references only for documents actually mentioned in the reply (or high-confidence vector matches)
        foreach (var so in topSOs)
        {
            if (reply.Contains(so.Item.OrderNumber, StringComparison.OrdinalIgnoreCase) || so.Score >= 0.65)
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
        }

        foreach (var po in topPOs)
        {
            if (reply.Contains(po.Item.PoNumber, StringComparison.OrdinalIgnoreCase) || po.Score >= 0.65)
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
        }

        return Ok(new ChatResponseDto
        {
            Reply = reply,
            ReferencedDocuments = referencedDocs.Take(6).ToList(),
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
            .Where(s => !s.IsDeleted && s.Status != "Cancelled")
            .ToListAsync();

        var validPOs = await _context.PurchaseOrders
            .AsNoTracking()
            .Include(p => p.Supplier)
            .Include(p => p.PurchaseOrderItems)
            .ThenInclude(i => i.Product)
            .Where(p => !p.IsDeleted && p.Status != "Cancelled")
            .ToListAsync();

        var totalRevenue = validSOs.Sum(s => s.TotalAmount);
        var totalExpense = validPOs.Sum(p => p.TotalAmount);
        var pendingSlips = validSOs.Count(s => s.PaymentStatus == "Checking");
        var pendingInboundPOs = validPOs.Count(p => p.Status == "Pending" || p.Status == "Approved");
        var pendingOutboundSOs = validSOs.Count(s => s.Status == "Pending" && s.PaymentStatus == "Paid");

        var lowStockProducts = await _context.Products
            .AsNoTracking()
            .Where(p => !p.IsDeleted && p.OnHandQuantity <= 5)
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
