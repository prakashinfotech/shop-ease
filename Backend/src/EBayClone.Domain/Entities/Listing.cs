using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class Listing : BaseEntity
{
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public ListingType ListingType { get; set; } = ListingType.FixedPrice;
    public decimal Price { get; set; }
    public decimal DiscountAmount { get; set; } = 0;
    public decimal? StartingBid { get; set; }
    public decimal? ReservePrice { get; set; }
    public decimal? BuyItNowPrice { get; set; }
    public DateTime? AuctionStartAt { get; set; }
    public DateTime? AuctionEndAt { get; set; }
    public int Quantity { get; set; } = 1;
    public bool FreeShipping { get; set; } = false;
    public ListingStatus Status { get; set; } = ListingStatus.Draft;
    public string? PrimaryImageUrl { get; set; }

    public Guid SellerId { get; set; }
    public User Seller { get; set; } = null!;

    public Guid? CategoryId { get; set; }
    public Category? Category { get; set; }

    public bool HasPendingVersion { get; set; } = false;

    // Auction live state
    public decimal? CurrentBidAmount { get; set; }
    public Guid? CurrentBidderId { get; set; }
    public int BidCount { get; set; }
    public decimal MinBidIncrement { get; set; } = 1.00m;
    public bool AutoExtendOnBid { get; set; } = true;

    public ICollection<ListingImage> Images { get; set; } = new List<ListingImage>();
    public ICollection<ListingAttributeValue> AttributeValues { get; set; } = new List<ListingAttributeValue>();
    public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    public ICollection<CartItem> CartItems { get; set; } = new List<CartItem>();
    public ICollection<ListingView> Views { get; set; } = new List<ListingView>();
    public ICollection<ListingVersion> Versions { get; set; } = new List<ListingVersion>();
    public ICollection<ListingApprovalLog> ApprovalLogs { get; set; } = new List<ListingApprovalLog>();
    public ICollection<Bid> Bids { get; set; } = new List<Bid>();
    public AuctionResult? AuctionResult { get; set; }
}
