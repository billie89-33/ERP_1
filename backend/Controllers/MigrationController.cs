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
                    string sku = ExtractString(doc, new[] { "sku", "itemCode" });
                    if (string.IsNullOrEmpty(sku)) continue; 

                    // เช็ค SKU ซ้ำในระบบ
                    if (await _context.Products.AnyAsync(p => p.Sku == sku))
                    {
                        sku = sku + "-" + Guid.NewGuid().ToString().Substring(0, 4).ToUpper();
                    }

                    // Extract Category Name
                    string categoryName = ExtractString(doc, new[] { "category", "categoryName" });
                    if (string.IsNullOrEmpty(categoryName)) categoryName = "Uncategorized";

                    // Find or Create Category
                    var category = await _context.Categories.FirstOrDefaultAsync(c => c.Name.ToLower() == categoryName.ToLower());
                    if (category == null)
                    {
                        category = new Category { Name = categoryName };
                        _context.Categories.Add(category);
                        await _context.SaveChangesAsync();
                    }

                    string brand = ExtractString(doc, new[] { "brand" });
                    string modelName = ExtractString(doc, new[] { "modelName", "model" });
                    string description = ExtractString(doc, new[] { "description" });

                    string name = ExtractString(doc, new[] { "name", "title", "productName" });
                    if (string.IsNullOrEmpty(name)) 
                    {
                        name = $"{brand} {modelName}".Trim();
                        if (string.IsNullOrEmpty(name)) name = "Unknown Product";
                    }

                    // 3. ดึง Price & Cost (พยายามแปลงเป็นตัวเลข)
                    decimal price = ExtractDecimal(doc, new[] { "price", "retailPrice", "sellPrice" });
                    decimal cost = ExtractDecimal(doc, new[] { "cost", "buyPrice", "capital" });
                    
                    // 4. Stock & Stats
                    int stock = (int)ExtractDecimal(doc, new[] { "stock", "stockQuantity", "qty", "quantity" });
                    int soldCount = (int)ExtractDecimal(doc, new[] { "soldCount" });
                    int viewCount = (int)ExtractDecimal(doc, new[] { "viewCount" });
                    bool isFeatured = doc.Contains("isFeatured") && !doc["isFeatured"].IsBsonNull && doc["isFeatured"].AsBoolean;
                    string status = ExtractString(doc, new[] { "status" });
                    if (string.IsNullOrEmpty(status)) status = "ACTIVE";

                    string[] tags = Array.Empty<string>();
                    if (doc.Contains("tags") && doc["tags"].IsBsonArray)
                    {
                        tags = doc["tags"].AsBsonArray.Select(t => t.AsString).ToArray();
                    }

                    // 5. Image (nested object)
                    string? imageUrl = null;
                    string? publicId = null;
                    if (doc.Contains("image") && doc["image"].IsBsonDocument)
                    {
                        var imgDoc = doc["image"].AsBsonDocument;
                        if (imgDoc.Contains("url") && !imgDoc["url"].IsBsonNull) imageUrl = imgDoc["url"].AsString;
                        if (imgDoc.Contains("publicId") && !imgDoc["publicId"].IsBsonNull) publicId = imgDoc["publicId"].AsString;
                    }
                    else
                    {
                        imageUrl = ExtractString(doc, new[] { "imageUrl", "image", "photo" });
                    }

                    // 6. Specifications
                    string specsJson = "{}";
                    if (doc.Contains("specifications") && doc["specifications"].IsBsonDocument)
                    {
                        specsJson = doc["specifications"].AsBsonDocument.ToJson();
                    }
                    else
                    {
                        // Fallback: Use remaining fields if no nested specs object
                        doc.Remove("_id"); 
                        doc.Remove("sku");
                        doc.Remove("name");
                        doc.Remove("price");
                        doc.Remove("cost");
                        doc.Remove("stock");
                        doc.Remove("brand");
                        doc.Remove("modelName");
                        doc.Remove("description");
                        doc.Remove("tags");
                        doc.Remove("status");
                        doc.Remove("isFeatured");
                        doc.Remove("soldCount");
                        doc.Remove("viewCount");
                        doc.Remove("image");
                        doc.Remove("imageUrl");
                        doc.Remove("category");
                        doc.Remove("createdAt");
                        doc.Remove("updatedAt");
                        doc.Remove("__v");
                        specsJson = doc.ToJson();
                    }

                    var newProduct = new Product
                    {
                        Sku = sku,
                        Name = name,
                        Brand = brand,
                        ModelName = modelName,
                        Description = description,
                        Price = price,
                        Cost = cost,
                        OnHandQuantity = stock,
                        ReservedQuantity = 0,
                        SoldCount = soldCount,
                        ViewCount = viewCount,
                        IsFeatured = isFeatured,
                        Status = status,
                        Tags = tags,
                        Image = new ProductImage 
                        {
                            Url = imageUrl ?? "",
                            PublicId = publicId ?? ""
                        },
                        CategoryId = category.Id,
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

    [HttpGet("peek-mongo")]
    public async Task<IActionResult> PeekMongo()
    {
        var mongoConnectionString = Environment.GetEnvironmentVariable("MongoDb__ConnectionString") ?? _configuration["MongoDb:ConnectionString"];
        var mongoDbName = Environment.GetEnvironmentVariable("MongoDb__DatabaseName") ?? _configuration["MongoDb:DatabaseName"];
        var client = new MongoClient(mongoConnectionString);
        var database = client.GetDatabase(mongoDbName);
        var collection = database.GetCollection<BsonDocument>("products");
        var doc = await collection.Find(new BsonDocument()).FirstOrDefaultAsync();
        if (doc == null) return NotFound("No documents found in MongoDB.");
        
        // Remove _id for easier JSON serialization
        doc.Remove("_id");
        return Ok(doc.ToJson());
    }

    [HttpPost("clear-products")]
    public async Task<IActionResult> ClearProducts()
    {
        _context.Products.RemoveRange(_context.Products);
        await _context.SaveChangesAsync();
        return Ok(new { message = "All products cleared from PostgreSQL." });
    }

    [HttpPost("fix-images")]
    public async Task<IActionResult> FixImages()
    {
        var products = await _context.Products.ToListAsync();
        int fixedCount = 0;
        foreach (var p in products)
        {
            if (p.Image == null) p.Image = new ProductImage();
            
            if (string.IsNullOrEmpty(p.Image.Url) && p.Specifications != null)
            {
                if (p.Specifications.RootElement.TryGetProperty("image", out var imageObj) && imageObj.ValueKind == JsonValueKind.Object)
                {
                    if (imageObj.TryGetProperty("url", out var urlProp) && urlProp.ValueKind == JsonValueKind.String)
                    {
                        p.Image.Url = urlProp.GetString() ?? "";
                        fixedCount++;
                    }
                    if (imageObj.TryGetProperty("publicId", out var pidProp) && pidProp.ValueKind == JsonValueKind.String)
                    {
                        p.Image.PublicId = pidProp.GetString() ?? "";
                    }
                }
            }
        }
        await _context.SaveChangesAsync();
        return Ok(new { message = $"Successfully fixed images for {fixedCount} products." });
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

    [HttpPost("seed-erp-data")]
    public async Task<IActionResult> SeedErpData()
    {
        try
        {
            // 1. Ensure User
            var adminUser = await _context.Users.FirstOrDefaultAsync(u => u.Role == "Admin");
            if (adminUser == null)
            {
                adminUser = new User { Username = "admin", PasswordHash = "dummy", Role = "Admin" };
                _context.Users.Add(adminUser);
                await _context.SaveChangesAsync();
            }

            // 2. Ensure Supplier
            var supplier = await _context.Suppliers.FirstOrDefaultAsync();
            if (supplier == null)
            {
                supplier = new Supplier { CompanyName = "Tech Supply Co., Ltd.", ContactName = "Mr. John", Phone = "0123456789" };
                _context.Suppliers.Add(supplier);
            }

            // 3. Ensure Customer
            var customer = await _context.Customers.FirstOrDefaultAsync();
            if (customer == null)
            {
                customer = new Customer { CompanyName = "Retail Shop A", TaxId = "1234567890123", Address = "Bangkok", CreditTermDays = 30, CreditLimit = 50000 };
                _context.Customers.Add(customer);
            }

            await _context.SaveChangesAsync();

            // 4. Get some products
            var products = await _context.Products.Take(3).ToListAsync();
            if (products.Count == 0)
            {
                var category = new Category { Name = "General" };
                _context.Categories.Add(category);
                
                products.Add(new Product { Sku = "DUMMY-1", Name = "Dummy Product 1", Price = 1000, Cost = 800, Category = category });
                products.Add(new Product { Sku = "DUMMY-2", Name = "Dummy Product 2", Price = 2000, Cost = 1500, Category = category });
                _context.Products.AddRange(products);
                await _context.SaveChangesAsync();
            }

            // 5. Create Dummy PO (Pending)
            var po1 = new PurchaseOrder
            {
                PoNumber = "PO" + DateTime.UtcNow.ToString("yyyyMMdd") + "-001",
                OrderDate = DateTime.UtcNow.AddDays(-2),
                Status = "Pending",
                SupplierId = supplier.Id,
                CreatedByUserId = adminUser.Id,
                TotalAmount = products[0].Cost * 50
            };
            po1.PurchaseOrderItems.Add(new PurchaseOrderItem { ProductId = products[0].Id, Quantity = 50, UnitCost = products[0].Cost });
            _context.PurchaseOrders.Add(po1);

            // 6. Create Dummy PO (Received) + Goods Receipt
            var po2 = new PurchaseOrder
            {
                PoNumber = "PO" + DateTime.UtcNow.ToString("yyyyMMdd") + "-002",
                OrderDate = DateTime.UtcNow.AddDays(-5),
                Status = "Received",
                SupplierId = supplier.Id,
                CreatedByUserId = adminUser.Id,
                TotalAmount = products.Sum(p => p.Cost * 10)
            };
            foreach (var p in products)
            {
                po2.PurchaseOrderItems.Add(new PurchaseOrderItem { ProductId = p.Id, Quantity = 10, UnitCost = p.Cost });
                p.OnHandQuantity += 10; // increase stock
            }
            _context.PurchaseOrders.Add(po2);

            var gr = new GoodsReceipt
            {
                GrNumber = "GR" + DateTime.UtcNow.ToString("yyyyMMdd") + "-001",
                ReceiptDate = DateTime.UtcNow.AddDays(-3),
                Status = "Completed",
                PurchaseOrder = po2,
                ReceivedByUserId = adminUser.Id
            };
            foreach (var item in po2.PurchaseOrderItems)
            {
                gr.GoodsReceiptItems.Add(new GoodsReceiptItem { ProductId = item.ProductId, ReceivedQuantity = item.Quantity });
            }
            _context.GoodsReceipts.Add(gr);

            // 7. Create Dummy SO (Pending)
            var so1 = new SalesOrder
            {
                OrderNumber = "SO" + DateTime.UtcNow.ToString("yyyyMMdd") + "-001",
                OrderDate = DateTime.UtcNow.AddDays(-1),
                Status = "Pending",
                CustomerId = customer.Id,
                CreatedByUserId = adminUser.Id,
                TotalAmount = products[0].Price * 2
            };
            so1.SalesOrderItems.Add(new SalesOrderItem { ProductId = products[0].Id, Quantity = 2, UnitPrice = products[0].Price });
            products[0].ReservedQuantity += 2; // reserve stock
            _context.SalesOrders.Add(so1);

            // 8. Create Dummy SO (Shipped) + Goods Issue
            var so2 = new SalesOrder
            {
                OrderNumber = "SO" + DateTime.UtcNow.ToString("yyyyMMdd") + "-002",
                OrderDate = DateTime.UtcNow.AddDays(-4),
                Status = "Shipped",
                CustomerId = customer.Id,
                CreatedByUserId = adminUser.Id,
                TotalAmount = products.Sum(p => p.Price * 1)
            };
            foreach (var p in products)
            {
                so2.SalesOrderItems.Add(new SalesOrderItem { ProductId = p.Id, Quantity = 1, UnitPrice = p.Price });
                // We don't increase ReservedQuantity because it's already shipped and deducted from OnHand
                p.OnHandQuantity -= 1;
                // If it was reserved previously, it would have been added and then removed. So net change is OnHand -= 1.
            }
            _context.SalesOrders.Add(so2);

            var gi = new GoodsIssue
            {
                GiNumber = "GI" + DateTime.UtcNow.ToString("yyyyMMdd") + "-001",
                IssueDate = DateTime.UtcNow.AddDays(-2),
                Status = "Shipped",
                SalesOrder = so2,
                IssuedByUserId = adminUser.Id
            };
            foreach (var item in so2.SalesOrderItems)
            {
                gi.GoodsIssueItems.Add(new GoodsIssueItem { ProductId = item.ProductId, IssuedQuantity = item.Quantity });
            }
            _context.GoodsIssues.Add(gi);

            await _context.SaveChangesAsync();

            return Ok(new { message = "ERP Dummy Data Seeded Successfully!" });
        }
        catch (Exception ex)
        {
            return StatusCode(500, new { message = "Failed to seed data.", error = ex.Message });
        }
    }
}
