using EBayClone.Application.DTOs.Listings;

namespace EBayClone.Application.DTOs.Admin;

public record AdminStatsResponse(
    int TotalUsers,
    int ActiveListings,
    int TotalOrders,
    decimal TotalRevenue
);

public record AdminUserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    int AccountType,
    string Role,
    bool IsEmailVerified,
    bool IsSuspended,
    bool IsDeleted,
    DateTime CreatedAt
);

public record AdminListingResponse(
    Guid Id,
    string Title,
    string SellerName,
    decimal Price,
    decimal DiscountAmount,
    decimal FinalPrice,
    int Status,
    bool HasPendingVersion,
    bool IsDeleted,
    DateTime CreatedAt
);

public record AdminListingDetailResponse(
    Guid Id,
    string Title,
    string Description,
    string SellerName,
    string SellerEmail,
    decimal Price,
    decimal DiscountAmount,
    decimal FinalPrice,
    int Status,
    bool HasPendingVersion,
    bool IsDeleted,
    DateTime CreatedAt,
    ListingVersionResponse? PendingVersion
);

public record AdminOrderResponse(
    Guid Id,
    string OrderNumber,
    string BuyerName,
    int ItemCount,
    decimal TotalAmount,
    int Status,
    string PaymentMethod,
    int PaymentStatus,
    string? Carrier,
    string? TrackingNumber,
    DateTime CreatedAt
);

public record AdminUsersQuery(
    int Page = 1,
    int PageSize = 15,
    string? Search = null,
    int? AccountType = null,
    string? Role = null,
    string? Status = null,
    string SortBy = "createdAt",
    string SortDirection = "desc"
);

public record AdminListingsQuery(
    int Page = 1,
    int PageSize = 15,
    string? Search = null,
    int? Status = null,
    string? Visibility = null,
    string SortBy = "createdAt",
    string SortDirection = "desc"
);

public record AdminOrdersQuery(
    int Page = 1,
    int PageSize = 15,
    string? Search = null,
    int? Status = null,
    string SortBy = "createdAt",
    string SortDirection = "desc"
);
