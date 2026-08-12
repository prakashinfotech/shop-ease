namespace EBayClone.Application.DTOs.Listings;

public record AutocompleteResponse(IReadOnlyCollection<string> Suggestions);

public record SearchFacetsResponse(
    IReadOnlyCollection<AttributeFacet> AttributeFacets,
    PriceFacet? Price
);

public record AttributeFacet(
    Guid AttributeId,
    string Name,
    string DisplayName,
    int DataType,
    IReadOnlyCollection<FacetOption> Options
);

public record FacetOption(string Value, string Label, int Count);

public record PriceFacet(decimal Min, decimal Max);

public record ListingResponse(
    Guid Id,
    string Title,
    string Description,
    int ListingType,
    decimal Price,
    decimal DiscountAmount,
    decimal FinalPrice,
    decimal? StartingBid,
    decimal? ReservePrice,
    decimal? BuyItNowPrice,
    DateTime? AuctionStartAt,
    DateTime? AuctionEndAt,
    int Quantity,
    bool FreeShipping,
    int Status,
    bool IsDeleted,
    string? PrimaryImageUrl,
    Guid SellerId,
    string SellerName,
    Guid? CategoryId,
    string? CategoryName,
    IReadOnlyCollection<ListingAttributeValueResponse> AttributeValues,
    IReadOnlyCollection<ListingImageResponse> Images,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    bool HasPendingVersion = false
);

public record ListingAttributeValueResponse(
    Guid CategoryAttributeId,
    string AttributeName,
    string DisplayName,
    int DataType,
    string Value,
    string? DisplayValue
);

public record ListingImageResponse(
    Guid Id,
    string Url,
    string? AltText,
    int SortOrder
);
