using System;
using System.Collections.Generic;

namespace JamineERP.Backend.Models;

public class GoodsReceipt : BaseEntity
{
    public string GrNumber { get; set; } = string.Empty;
    public DateTime ReceiptDate { get; set; }
    public string Status { get; set; } = "Draft"; // "Draft", "Completed"
    public string? Remarks { get; set; }

    // Relationship to Purchase Order
    public Guid PurchaseOrderId { get; set; }
    public PurchaseOrder PurchaseOrder { get; set; } = null!;

    // Receiver (Warehouse Staff)
    public Guid ReceivedByUserId { get; set; }
    public User ReceivedByUser { get; set; } = null!;

    public ICollection<GoodsReceiptItem> GoodsReceiptItems { get; set; } = new List<GoodsReceiptItem>();
}
