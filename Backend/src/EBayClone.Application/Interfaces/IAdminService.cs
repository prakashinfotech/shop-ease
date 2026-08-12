using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Admin;

namespace EBayClone.Application.Interfaces;

public interface IAdminService
{
    Task<AdminStatsResponse> GetStatsAsync(CancellationToken ct = default);
    Task<PagedResult<AdminUserResponse>> GetUsersAsync(AdminUsersQuery query, CancellationToken ct = default);
    Task<PagedResult<AdminListingResponse>> GetListingsAsync(AdminListingsQuery query, CancellationToken ct = default);
    Task<PagedResult<AdminOrderResponse>> GetOrdersAsync(AdminOrdersQuery query, CancellationToken ct = default);
    Task<AdminListingDetailResponse> GetListingDetailAsync(Guid id, CancellationToken ct = default);
    Task DeleteListingAsync(Guid id, CancellationToken ct = default);
}
