namespace EBayClone.Application.DTOs.Auctions;

public record PlaceBidRequest(decimal Amount);

public record BidResponse(
    Guid Id,
    Guid ListingId,
    string BidderHandle,
    decimal Amount,
    bool IsWinning,
    DateTime PlacedAt
);

public record AuctionStatusResponse(
    Guid ListingId,
    string Title,
    decimal? StartingBid,
    decimal? CurrentBidAmount,
    decimal? ReservePrice,
    bool ReserveMet,
    decimal? BuyItNowPrice,
    decimal MinBidIncrement,
    int BidCount,
    DateTime? AuctionStartAt,
    DateTime? AuctionEndAt,
    int Status,
    bool IsCurrentUserWinning = false
);

public record AuctionBidPlacedEvent(
    Guid ListingId,
    decimal Amount,
    int BidCount,
    DateTime? AuctionEndAt,
    string BidderHandle
);

public record AuctionExtendRequest(int Minutes);

public record AdminAuctionResponse(
    Guid ListingId,
    string Title,
    decimal? StartingBid,
    decimal? CurrentBidAmount,
    decimal? ReservePrice,
    int BidCount,
    DateTime? AuctionEndAt,
    int Status,
    string SellerName,
    Guid SellerId
);

public record AdminBidResponse(
    Guid Id,
    Guid ListingId,
    Guid BidderId,
    string BidderName,
    string BidderEmail,
    decimal Amount,
    bool IsWinning,
    DateTime PlacedAt
);

public record AdminAuctionsQuery(int Page, int PageSize, int? Status);
