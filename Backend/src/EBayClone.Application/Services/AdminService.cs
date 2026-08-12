using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Admin;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Application.Services;

public class AdminService(
    IRepository<User> userRepository,
    IRepository<Listing> listingRepository,
    IRepository<Order> orderRepository,
    IListingApprovalService listingApprovalService) : IAdminService
{
    private const string SortStatus = "status";
    public async Task<AdminStatsResponse> GetStatsAsync(CancellationToken ct = default)
    {
        var totalUsers = await userRepository.CountAsync(ct: ct);
        var activeListings = await listingRepository.CountAsync(
            l => l.Status == ListingStatus.Active, ct);
        var totalOrders = await orderRepository.CountAsync(ct: ct);
        var totalRevenue = await orderRepository.Query()
            .Where(o => o.Status == OrderStatus.Delivered)
            .SumAsync(o => o.TotalAmount, ct);

        return new AdminStatsResponse(totalUsers, activeListings, totalOrders, totalRevenue);
    }

    public async Task<PagedResult<AdminUserResponse>> GetUsersAsync(AdminUsersQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);

        var q = userRepository.Query().IgnoreQueryFilters().AsNoTracking()
            .Where(u => u.Role != UserRole.Admin);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(u => u.Email.Contains(query.Search) ||
                u.FirstName.Contains(query.Search) ||
                u.LastName.Contains(query.Search));

        if (query.AccountType.HasValue && Enum.IsDefined(typeof(AccountType), query.AccountType.Value))
            q = q.Where(u => u.AccountType == (AccountType)query.AccountType.Value);

        if (!string.IsNullOrWhiteSpace(query.Role) && Enum.TryParse<UserRole>(query.Role, true, out var parsedRole))
            q = q.Where(u => u.Role == parsedRole);

        q = query.Status?.ToLowerInvariant() switch
        {
            "active" => q.Where(u => !u.IsDeleted && !u.IsSuspended),
            "suspended" => q.Where(u => u.IsSuspended),
            "deleted" => q.Where(u => u.IsDeleted),
            "unverified" => q.Where(u => !u.IsEmailVerified),
            _ => q,
        };

        var total = await q.CountAsync(ct);

        q = (query.SortBy.ToLowerInvariant(), query.SortDirection.ToLowerInvariant()) switch
        {
            ("name", "asc") => q.OrderBy(u => u.FirstName).ThenBy(u => u.LastName),
            ("name", _) => q.OrderByDescending(u => u.FirstName).ThenByDescending(u => u.LastName),
            ("email", "asc") => q.OrderBy(u => u.Email),
            ("email", _) => q.OrderByDescending(u => u.Email),
            ("accounttype", "asc") => q.OrderBy(u => u.AccountType),
            ("accounttype", _) => q.OrderByDescending(u => u.AccountType),
            ("role", "asc") => q.OrderBy(u => u.Role),
            ("role", _) => q.OrderByDescending(u => u.Role),
            (SortStatus, "asc") => q.OrderBy(u => u.IsDeleted).ThenBy(u => u.IsSuspended).ThenByDescending(u => u.IsEmailVerified),
            (SortStatus, _) => q.OrderByDescending(u => u.IsDeleted).ThenByDescending(u => u.IsSuspended).ThenBy(u => u.IsEmailVerified),
            ("createdat", "asc") => q.OrderBy(u => u.CreatedAt),
            _ => q.OrderByDescending(u => u.CreatedAt),
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new AdminUserResponse(
                u.Id,
                u.FirstName,
                u.LastName,
                u.Email,
                (int)u.AccountType,
                u.Role.ToString(),
                u.IsEmailVerified,
                u.IsSuspended,
                u.IsDeleted,
                u.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<AdminUserResponse>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<AdminListingResponse>> GetListingsAsync(AdminListingsQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);

        var q = listingRepository.Query().Include(l => l.Seller).IgnoreQueryFilters().AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(l => l.Title.Contains(query.Search) ||
                l.Seller.Email.Contains(query.Search) ||
                l.Seller.FirstName.Contains(query.Search) ||
                l.Seller.LastName.Contains(query.Search));

        if (query.Status.HasValue && Enum.IsDefined(typeof(ListingStatus), query.Status.Value))
        {
            var listingStatus = (ListingStatus)query.Status.Value;
            q = listingStatus == ListingStatus.PendingApproval
                ? q.Where(l => l.Status == ListingStatus.PendingApproval || l.HasPendingVersion)
                : q.Where(l => l.Status == listingStatus);
        }
        else
        {
            q = q.Where(l => l.Status != ListingStatus.PendingApproval && !l.HasPendingVersion);
        }

        q = query.Visibility?.ToLowerInvariant() switch
        {
            "active" => q.Where(l => !l.IsDeleted),
            "deleted" => q.Where(l => l.IsDeleted),
            _ => q,
        };

        var total = await q.CountAsync(ct);

        q = (query.SortBy.ToLowerInvariant(), query.SortDirection.ToLowerInvariant()) switch
        {
            ("title", "asc") => q.OrderBy(l => l.Title),
            ("title", _) => q.OrderByDescending(l => l.Title),
            ("seller", "asc") => q.OrderBy(l => l.Seller.FirstName).ThenBy(l => l.Seller.LastName),
            ("seller", _) => q.OrderByDescending(l => l.Seller.FirstName).ThenByDescending(l => l.Seller.LastName),
            ("price", "asc") => q.OrderBy(l => l.Price - l.DiscountAmount),
            ("price", _) => q.OrderByDescending(l => l.Price - l.DiscountAmount),
            (SortStatus, "asc") => q.OrderBy(l => l.Status),
            (SortStatus, _) => q.OrderByDescending(l => l.Status),
            ("updatedat", "asc") => q.OrderBy(l => l.UpdatedAt),
            ("updatedat", _) => q.OrderByDescending(l => l.UpdatedAt),
            ("createdat", "asc") => q.OrderBy(l => l.CreatedAt),
            _ => q.OrderByDescending(l => l.CreatedAt),
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(l => new AdminListingResponse(
                l.Id,
                l.Title,
                $"{l.Seller.FirstName} {l.Seller.LastName}",
                l.Price,
                l.DiscountAmount,
                l.Price - l.DiscountAmount,
                (int)l.Status,
                l.HasPendingVersion,
                l.IsDeleted,
                l.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<AdminListingResponse>.Create(items, total, page, pageSize);
    }

    public async Task<PagedResult<AdminOrderResponse>> GetOrdersAsync(AdminOrdersQuery query, CancellationToken ct = default)
    {
        var (page, pageSize) = NormalizePaging(query.Page, query.PageSize);

        var q = orderRepository.Query()
            .Include(o => o.Buyer)
            .Include(o => o.Items)
            .IgnoreQueryFilters()
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(o => o.OrderNumber.Contains(query.Search) ||
                o.Buyer.Email.Contains(query.Search) ||
                o.Buyer.FirstName.Contains(query.Search) ||
                o.Buyer.LastName.Contains(query.Search));

        if (query.Status.HasValue && Enum.IsDefined(typeof(OrderStatus), query.Status.Value))
            q = q.Where(o => o.Status == (OrderStatus)query.Status.Value);

        var total = await q.CountAsync(ct);

        q = (query.SortBy.ToLowerInvariant(), query.SortDirection.ToLowerInvariant()) switch
        {
            ("ordernumber", "asc") => q.OrderBy(o => o.OrderNumber),
            ("ordernumber", _) => q.OrderByDescending(o => o.OrderNumber),
            ("buyer", "asc") => q.OrderBy(o => o.Buyer.FirstName).ThenBy(o => o.Buyer.LastName),
            ("buyer", _) => q.OrderByDescending(o => o.Buyer.FirstName).ThenByDescending(o => o.Buyer.LastName),
            ("itemcount", "asc") => q.OrderBy(o => o.Items.Count),
            ("itemcount", _) => q.OrderByDescending(o => o.Items.Count),
            ("totalamount", "asc") => q.OrderBy(o => o.TotalAmount),
            ("totalamount", _) => q.OrderByDescending(o => o.TotalAmount),
            (SortStatus, "asc") => q.OrderBy(o => o.Status),
            (SortStatus, _) => q.OrderByDescending(o => o.Status),
            ("createdat", "asc") => q.OrderBy(o => o.CreatedAt),
            _ => q.OrderByDescending(o => o.CreatedAt),
        };

        var items = await q
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(o => new AdminOrderResponse(
                o.Id,
                o.OrderNumber,
                $"{o.Buyer.FirstName} {o.Buyer.LastName}",
                o.Items.Count,
                o.TotalAmount,
                (int)o.Status,
                o.PaymentMethod,
                (int)o.PaymentStatus,
                o.Carrier,
                o.TrackingNumber,
                o.CreatedAt))
            .ToListAsync(ct);

        return PagedResult<AdminOrderResponse>.Create(items, total, page, pageSize);
    }

    public async Task<AdminListingDetailResponse> GetListingDetailAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await listingRepository.Query()
            .Include(l => l.Seller)
            .IgnoreQueryFilters()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException("Listing not found.");

        var pendingVersion = await listingApprovalService.GetPendingVersionAsync(id, ct);

        return new AdminListingDetailResponse(
            listing.Id,
            listing.Title,
            listing.Description,
            $"{listing.Seller.FirstName} {listing.Seller.LastName}",
            listing.Seller.Email,
            listing.Price,
            listing.DiscountAmount,
            listing.Price - listing.DiscountAmount,
            (int)listing.Status,
            listing.HasPendingVersion,
            listing.IsDeleted,
            listing.CreatedAt,
            pendingVersion);
    }

    public async Task DeleteListingAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await listingRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException("Listing not found.");

        listingRepository.SoftDelete(listing);
        await listingRepository.SaveChangesAsync(ct);
    }

    private static (int Page, int PageSize) NormalizePaging(int page, int pageSize)
    {
        page = page < 1 ? 1 : page;
        pageSize = pageSize switch
        {
            < 1 => 15,
            > 100 => 100,
            _ => pageSize,
        };
        return (page, pageSize);
    }
}
