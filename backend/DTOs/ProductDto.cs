using System.Text.Json;

namespace JamineERP.Backend.DTOs;

public class ProductDto
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

    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = "ACTIVE";
    public bool IsFeatured { get; set; }
    public int SoldCount { get; set; }
    public int ViewCount { get; set; }

    public JsonElement? Specifications { get; set; }
    public ProductImageDto? Image { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class ProductImageDto
{
    public string Url { get; set; } = string.Empty;
    public string PublicId { get; set; } = string.Empty;
}

public class CreateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Brand { get; set; } = string.Empty;
    public string ModelName { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public string[] Tags { get; set; } = Array.Empty<string>();
    public string Status { get; set; } = "ACTIVE";
    public bool IsFeatured { get; set; }
    public JsonElement? Specifications { get; set; }
    public Guid CategoryId { get; set; }
    public ProductImageDto? Image { get; set; }
}

public class UpdateProductDto : CreateProductDto
{
}

public class PatchProductPriceDto
{
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
}
