using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class Quotation : BaseEntity
{
    public string QuoteNumber { get; set; } = string.Empty;
    public DateTime QuoteDate { get; set; }
    public DateTime ValidUntil { get; set; }
    public string Status { get; set; } = string.Empty; // "Draft, Sent, Accepted"
    public decimal TotalAmount { get; set; }

    public Guid CustomerId { get; set; }
    public Customer Customer { get; set; } = null!;

    public ICollection<QuotationItem> QuotationItems { get; set; } = new List<QuotationItem>();
}
