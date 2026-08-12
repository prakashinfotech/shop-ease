using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class CartItem : BaseEntity
{
    public int Quantity { get; set; } = 1;

    public Guid CartId { get; set; }
    public Cart Cart { get; set; } = null!;

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}