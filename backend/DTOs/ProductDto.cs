using System.Text.Json;

namespace JamineERP.Backend.DTOs;

public class ProductDto
{
    public Guid Id { get; set; }
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    
    public int OnHandQuantity { get; set; }
    public int ReservedQuantity { get; set; }
    public int AvailableQuantity { get; set; }

    public JsonElement? Specifications { get; set; }
    public string? ImageUrl { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
}

public class CreateProductDto
{
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
    public JsonElement? Specifications { get; set; }
    public Guid CategoryId { get; set; }
    public string? ImageUrl { get; set; }
    public string? CloudinaryPublicId { get; set; }
}

public class UpdateProductDto : CreateProductDto
{
}

public class PatchProductPriceDto
{
    public decimal Price { get; set; }
    public decimal Cost { get; set; }
}
