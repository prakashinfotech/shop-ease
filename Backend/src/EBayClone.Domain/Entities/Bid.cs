using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class Bid : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public Guid BidderId { get; set; }
    public User Bidder { get; set; } = null!;

    public decimal Amount { get; set; }
    public bool IsAutoBid { get; set; }
    public bool IsWinning { get; set; }
}
