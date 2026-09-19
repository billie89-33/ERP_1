using System;
using System.Collections.Generic;

namespace JamineERP.Backend.DTOs;

public class CreatePurchaseOrderDto
{
    public Guid SupplierId { get; set; }
    public List<CreatePurchaseOrderItemDto> Items { get; set; } = new();
}

public class CreatePurchaseOrderItemDto
{
    public Guid ProductId { get; set; }
    public int Quantity { get; set; }
    public decimal UnitCost { get; set; }
}

public class ReceivePurchaseOrderDto
{
    // A DTO if we separate the receiving process. But the requirement says:
    // ทำเมนูและ API ของระบบจัดซื้อ (PO) -> ไฮไลท์: เขียน try/catch + Transaction รับของเข้าเพื่อ +เพิ่มสต๊อก
}
