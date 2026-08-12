namespace EBayClone.Application.DTOs.Listings;

public record SubmitListingUpdateRequest(
    string Title,
    string Description,
    int ListingType,
    decimal Price,
    int Quantity,
    decimal DiscountAmount = 0,
    bool FreeShipping = false,
    Guid? CategoryId = null,
    decimal? StartingBid = null,
    decimal? ReservePrice = null,
    decimal? BuyItNowPrice = null,
    DateTime? AuctionStartAt = null,
    DateTime? AuctionEndAt = null,
    IReadOnlyCollection<ListingAttributeValueRequest>? AttributeValues = null,
    IReadOnlyCollection<ListingImageRequest>? Images = null
);

public record ApproveListingRequest(string? Notes = null);

public record RejectListingRequest(string Reason, string? Notes = null);

public record ListingVersionResponse(
    Guid Id,
    Guid ListingId,
    int VersionNumber,
    bool IsPendingUpdate,
    int Status,
    string StatusName,
    string? RejectionReason,
    string? ReviewedByAdminName,
    DateTime? ReviewedAt,
    DateTime SubmittedAt,
    ListingVersionSnapshotDto? Snapshot
);

public record ListingVersionSnapshotDto(
    string Title,
    string Description,
    int ListingType,
    decimal Price,
    decimal DiscountAmount,
    decimal? StartingBid,
    decimal? BuyItNowPrice,
    int Quantity,
    bool FreeShipping,
    string? CategoryName,
    IReadOnlyList<string> ImageUrls
);
