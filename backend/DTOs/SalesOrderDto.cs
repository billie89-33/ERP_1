using System;
using System.Collections.Generic;

namespace JamineERP.Backend.DTOs;

public class CreateSalesOrderDto
{
    public Guid CustomerId { get; set; }
    public List<CreateSalesOrderItemDto> Items { get; set; } = new();
}

public class CreateSalesOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
}
