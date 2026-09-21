using System;

namespace JamineERP.Backend.Models;

public class GoodsReceiptItem : BaseEntity
{
    public Guid GoodsReceiptId { get; set; }
    public GoodsReceipt GoodsReceipt { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int ReceivedQuantity { get; set; }
}
