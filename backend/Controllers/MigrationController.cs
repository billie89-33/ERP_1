using JamineERP.Backend.Data;
using JamineERP.Backend.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MongoDB.Bson;
using MongoDB.Driver;
using System.Text.Json;

namespace JamineERP.Backend.Controllers;

[Route("api/[controller]")]
[ApiController]
public class MigrationController : ControllerBase
{
    private readonly AppDbContext _context;
    private readonly IConfiguration _configuration;

    public MigrationController(AppDbContext context, IConfiguration configuration)
    {
        _context = context;
        _configuration = configuration;
    }

    [HttpPost("migrate-products")]
    public async Task<IActionResult> MigrateProducts()
    {
        try
        {
            var mongoConnectionString = Environment.GetEnvironmentVariable("MongoDb__ConnectionString") ?? _configuration["MongoDb:ConnectionString"];
            var mongoDbName = Environment.GetEnvironmentVariable("MongoDb__DatabaseName") ?? _configuration["MongoDb:DatabaseName"];

            if (string.IsNullOrEmpty(mongoConnectionString))
                return BadRequest("MongoDB ConnectionString is not configured.");

            var client = new MongoClient(mongoConnectionString);
            var database = client.GetDatabase(mongoDbName);
            var collection = database.GetCollection<BsonDocument>("products");

            // หา Default Category หรือสร้างใหม่ถ้าไม่มี
            var defaultCategory = await _context.Categories.FirstOrDefaultAsync(c => c.Name == "Migrated");
            if (defaultCategory == null)
            {
                defaultCategory = new Category { Name = "Migrated" };
                _context.Categories.Add(defaultCategory);
                await _context.SaveChangesAsync();
            }

            int migratedCount = 0;
            int batchSize = 500;
            
            // อ่านข้อมูลทั้งหมดเป็น Cursor แบบ Batch เพื่อป้องกัน RAM เต็ม
            using var cursor = await collection.FindAsync(new BsonDocument(), new FindOptions<BsonDocument> { BatchSize = batchSize });
            
            while (await cursor.MoveNextAsync())
            {
                var batch = cursor.Current;
                foreach (var doc in batch)
                {
                    // 1. ดึง Sku (เดาชื่อ Field)
                    string sku = ExtractString(doc, new[] { "sku", "code", "barcode", "product_code" });
                    if (string.IsNullOrEmpty(sku)) sku = "MIG-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper();

                    // เช็ค SKU ซ้ำในระบบ
                    if (await _context.Products.AnyAsync(p => p.Sku == sku))
                    {
                        sku = sku + "-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    }

                    // 2. ดึง Name
                    string name = ExtractString(doc, new[] { "name", "title", "productName", "description" });
                    if (string.IsNullOrEmpty(name)) name = "Unknown Product";

                    // 3. ดึง Price & Cost (พยายามแปลงเป็นตัวเลข)
                    decimal price = ExtractDecimal(doc, new[] { "price", "retailPrice", "sellPrice" });
                    decimal cost = ExtractDecimal(doc, new[] { "cost", "buyPrice", "capital" });
                    
                    // 4. ดึง Stock
                    int stock = (int)ExtractDecimal(doc, new[] { "stock", "stockQuantity", "qty", "quantity" });

                    // 5. ดึงเศษข้อมูลอื่นๆ มายัดลง Specifications
                    doc.Remove("_id"); // ลบ Primary key ของ Mongo ออก
                    doc.Remove("sku");
                    doc.Remove("name");
                    doc.Remove("price");
                    doc.Remove("cost");
                    doc.Remove("stock");

                    string specsJson = doc.ToJson();

                    var newProduct = new Product
                    {
                        Sku = sku,
                        Name = name,
                        Price = price,
                        Cost = cost,
                        StockQuantity = stock,
                        CategoryId = defaultCategory.Id,
                        Specifications = JsonDocument.Parse(specsJson)
                    };

                    _context.Products.Add(newProduct);
                    migratedCount++;
                }

                // เซฟทีละ Batch
                await _context.SaveChangesAsync();
            }

            return Ok(new { message = $"Successfully migrated {migratedCount} products to PostgreSQL." });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Migration failed.", error = ex.Message, stackTrace = ex.StackTrace });
        }
    }

    // --- Helper Methods สำหรับทำ Data Cleansing (ดัก Type ตีกัน) ---
    private string ExtractString(BsonDocument doc, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (doc.Contains(key) && !doc[key].IsBsonNull)
            {
                return doc[key].AsString;
            }
        }
        return string.Empty;
    }

    private decimal ExtractDecimal(BsonDocument doc, string[] possibleKeys)
    {
        foreach (var key in possibleKeys)
        {
            if (doc.Contains(key) && !doc[key].IsBsonNull)
            {
                var val = doc[key];
                if (val.IsNumeric) return (decimal)val.ToDouble();
                if (val.IsString)
                {
                    // ลบคอมม่าออกตาม Numeric Sanitization Pattern
                    string cleanVal = val.AsString.Replace(",", "").Trim();
                    if (decimal.TryParse(cleanVal, out decimal result)) return result;
                }
            }
        }
        return 0m;
    }
}
