using System;

namespace JamineERP.Backend.Models;

public class GoodsIssueItem : BaseEntity
{
    public Guid GoodsIssueId { get; set; }
    public GoodsIssue GoodsIssue { get; set; } = null!;

    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;

    public int IssuedQuantity { get; set; }
}
