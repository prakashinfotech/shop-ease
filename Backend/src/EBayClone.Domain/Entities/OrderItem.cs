using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class OrderItem : BaseEntity
{
    public int Quantity { get; set; }
    public decimal UnitPrice { get; set; }
    public string ListingTitle { get; set; } = string.Empty;

    public Guid OrderId { get; set; }
    public Order Order { get; set; } = null!;

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}