using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class Customer : BaseEntity
{
    public string CustomerType { get; set; } = "B2B"; // B2B or B2C
    public string CompanyName { get; set; } = string.Empty; // Also used for First/Last name in B2C
    public string TaxId { get; set; } = string.Empty;
    public int CreditTermDays { get; set; } = 0;
    public decimal CreditLimit { get; set; } = 0;
    public string Address { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public Guid? UserId { get; set; }

    public ICollection<Quotation> Quotations { get; set; } = new List<Quotation>();
    public ICollection<SalesOrder> SalesOrders { get; set; } = new List<SalesOrder>();
}
