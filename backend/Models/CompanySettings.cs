using System;
using System.ComponentModel.DataAnnotations;

namespace JamineERP.Backend.Models;

public class CompanySettings
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    
    [Required]
    public string CompanyName { get; set; } = string.Empty;
    
    public string TaxId { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string LogoUrl { get; set; } = string.Empty; // Base64 or URL

    // Storefront Hero Banner Customization
    public string HeroBadge { get; set; } = string.Empty;
    public string HeroTitle { get; set; } = string.Empty;
    public string HeroSubtitle { get; set; } = string.Empty;
    public string HeroImageUrl { get; set; } = string.Empty;
    public string HeroCtaText { get; set; } = string.Empty;
    
    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
