using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class Customer : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string TaxId { get; set; } = string.Empty;
    public int CreditTermDays { get; set; }
    public decimal CreditLimit { get; set; }
    public string Address { get; set; } = string.Empty;

    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
