using EBayClone.Domain.Enums;

namespace EBayClone.Application.DTOs.Listings;

public record CreateListingRequest(
    string Title,
    string Description,
    ListingType ListingType,
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

public record UpdateListingRequest(
    string Title,
    string Description,
    ListingType ListingType,
    decimal Price,
    int Quantity,
    decimal DiscountAmount = 0,
    bool FreeShipping = false,
    Guid? CategoryId = null,
    ListingStatus? Status = null,
    decimal? StartingBid = null,
    decimal? ReservePrice = null,
    decimal? BuyItNowPrice = null,
    DateTime? AuctionStartAt = null,
    DateTime? AuctionEndAt = null,
    IReadOnlyCollection<ListingAttributeValueRequest>? AttributeValues = null,
    IReadOnlyCollection<ListingImageRequest>? Images = null
);

public record ListingAttributeValueRequest(
    Guid CategoryAttributeId,
    string? Value
);

public record ListingImageRequest(
    string Url,
    string? AltText = null,
    int SortOrder = 0
);

public record ListingImageUploadResponse(
    string Url
);

public record ListingQuery(
    int Page = 1,
    int PageSize = 24,
    string? Search = null,
    string? Category = null,
    Guid? CategoryId = null,
    decimal? MinPrice = null,
    decimal? MaxPrice = null,
    bool? FreeShipping = null,
    ListingStatus? Status = null,
    ListingType? ListingType = null,
    Guid? SellerId = null,
    Guid? ExcludeSellerId = null,
    bool IncludeDeleted = false,
    string SortBy = "updatedAt",
    string SortDirection = "desc",
    Dictionary<string, string>? AttributeFilters = null
);
