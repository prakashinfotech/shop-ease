using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Admin;
using EBayClone.Application.DTOs.Auctions;
using EBayClone.Application.DTOs.BusinessProfile;
using EBayClone.Application.DTOs.Listings;
using EBayClone.Application.DTOs.Orders;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
[Authorize(Roles = "Admin")]
public class AdminController(
    IAdminService adminService,
    IUserService userService,
    IBusinessProfileService businessProfileService,
    IListingApprovalService listingApprovalService,
    IOrderService orderService,
    IAuctionService auctionService) : ControllerBase
{
    private const string DefaultSortBy = "createdAt";
    [HttpGet("stats")]
    public async Task<ActionResult<ApiResponse<AdminStatsResponse>>> GetStats(CancellationToken ct)
        => Ok(ApiResponse<AdminStatsResponse>.Ok(await adminService.GetStatsAsync(ct)));

    [HttpGet("users")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminUserResponse>>>> GetUsers(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null, [FromQuery] int? accountType = null,
        [FromQuery] string? role = null, [FromQuery] string? status = null,
        [FromQuery] string? sortBy = DefaultSortBy, [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        var query = new AdminUsersQuery(page, pageSize, search, accountType, role, status,
            sortBy ?? DefaultSortBy, sortDirection ?? "desc");
        return Ok(ApiResponse<PagedResult<AdminUserResponse>>.Ok(await adminService.GetUsersAsync(query, ct)));
    }

    [HttpPatch("users/{id:guid}/suspend")]
    public async Task<ActionResult> SuspendUser(Guid id, CancellationToken ct)
    {
        await userService.SuspendUserAsync(id, ct);
        return Ok(ApiResponse.Ok("User suspended"));
    }

    [HttpPatch("users/{id:guid}/activate")]
    public async Task<ActionResult> ActivateUser(Guid id, CancellationToken ct)
    {
        await userService.ActivateUserAsync(id, ct);
        return Ok(ApiResponse.Ok("User activated"));
    }

    [HttpGet("listings")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminListingResponse>>>> GetListings(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null, [FromQuery] int? status = null,
        [FromQuery] string? visibility = null,
        [FromQuery] string? sortBy = DefaultSortBy, [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        var query = new AdminListingsQuery(page, pageSize, search, status, visibility,
            sortBy ?? DefaultSortBy, sortDirection ?? "desc");
        return Ok(ApiResponse<PagedResult<AdminListingResponse>>.Ok(await adminService.GetListingsAsync(query, ct)));
    }

    [HttpGet("orders")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminOrderResponse>>>> GetOrders(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15,
        [FromQuery] string? search = null, [FromQuery] int? status = null,
        [FromQuery] string? sortBy = DefaultSortBy, [FromQuery] string? sortDirection = "desc",
        CancellationToken ct = default)
    {
        var query = new AdminOrdersQuery(page, pageSize, search, status,
            sortBy ?? DefaultSortBy, sortDirection ?? "desc");
        return Ok(ApiResponse<PagedResult<AdminOrderResponse>>.Ok(await adminService.GetOrdersAsync(query, ct)));
    }

    [HttpPut("orders/{id:guid}/status")]
    public async Task<ActionResult<ApiResponse<OrderResponse>>> UpdateOrderStatus(
        Guid id, [FromBody] UpdateOrderStatusRequest request, CancellationToken ct)
    {
        var result = await orderService.UpdateStatusAsync(id, request, ct);
        return Ok(ApiResponse<OrderResponse>.Ok(result, "Order status updated"));
    }

    [HttpGet("listings/{id:guid}")]
    public async Task<ActionResult<ApiResponse<AdminListingDetailResponse>>> GetListingDetail(Guid id, CancellationToken ct)
        => Ok(ApiResponse<AdminListingDetailResponse>.Ok(await adminService.GetListingDetailAsync(id, ct)));

    [HttpPost("listings/{id:guid}/approve")]
    public async Task<ActionResult<ApiResponse<ListingResponse>>> ApproveListing(
        Guid id, [FromBody] ApproveListingRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue("sub")!);
        var result = await listingApprovalService.ApproveAsync(id, adminId, request.Notes, ct);
        return Ok(ApiResponse<ListingResponse>.Ok(result, "Listing approved"));
    }

    [HttpPost("listings/{id:guid}/reject")]
    public async Task<ActionResult<ApiResponse<ListingResponse>>> RejectListing(
        Guid id, [FromBody] RejectListingRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue("sub")!);
        var result = await listingApprovalService.RejectAsync(id, adminId, request.Reason, request.Notes, ct);
        return Ok(ApiResponse<ListingResponse>.Ok(result, "Listing rejected"));
    }

    [HttpGet("listings/{id:guid}/versions")]
    public async Task<ActionResult<ApiResponse<IReadOnlyList<ListingVersionResponse>>>> GetListingVersions(Guid id, CancellationToken ct)
        => Ok(ApiResponse<IReadOnlyList<ListingVersionResponse>>.Ok(await listingApprovalService.GetVersionsAsync(id, ct)));

    [HttpDelete("listings/{id:guid}")]
    public async Task<ActionResult<ApiResponse>> DeleteListing(Guid id, CancellationToken ct)
    {
        await adminService.DeleteListingAsync(id, ct);
        return Ok(ApiResponse.Ok("Listing deleted"));
    }

    [HttpGet("business-profiles")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminBusinessProfileResponse>>>> GetBusinessProfiles(
        [FromQuery] PagedQuery query, [FromQuery] string? status, CancellationToken ct)
        => Ok(ApiResponse<PagedResult<AdminBusinessProfileResponse>>.Ok(
            await businessProfileService.GetAllAsync(query, status, ct)));

    [HttpGet("auctions")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminAuctionResponse>>>> GetAuctions(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 15, [FromQuery] int? status = null,
        CancellationToken ct = default)
    {
        var query = new AdminAuctionsQuery(page, pageSize, status);
        return Ok(ApiResponse<PagedResult<AdminAuctionResponse>>.Ok(await auctionService.GetAdminAuctionsAsync(query, ct)));
    }

    [HttpGet("auctions/{listingId:guid}/bids")]
    public async Task<ActionResult<ApiResponse<PagedResult<AdminBidResponse>>>> GetAuctionBids(
        Guid listingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 50, CancellationToken ct = default)
        => Ok(ApiResponse<PagedResult<AdminBidResponse>>.Ok(await auctionService.GetAdminBidHistoryAsync(listingId, page, pageSize, ct)));

    [HttpPost("auctions/{listingId:guid}/cancel")]
    public async Task<ActionResult<ApiResponse>> CancelAuction(Guid listingId, CancellationToken ct)
    {
        await auctionService.CancelAuctionAsync(listingId, ct);
        return Ok(ApiResponse.Ok("Auction cancelled"));
    }

    [HttpPost("auctions/{listingId:guid}/extend")]
    public async Task<ActionResult<ApiResponse>> ExtendAuction(
        Guid listingId, [FromBody] AuctionExtendRequest request, CancellationToken ct)
    {
        await auctionService.ExtendAuctionAsync(listingId, request.Minutes, ct);
        return Ok(ApiResponse.Ok($"Auction extended by {request.Minutes} minutes"));
    }

    [HttpPut("business-profiles/{id:guid}/review")]
    public async Task<ActionResult<ApiResponse<BusinessProfileResponse>>> ReviewBusinessProfile(
        Guid id, [FromBody] ReviewBusinessProfileRequest request, CancellationToken ct)
    {
        var adminId = Guid.Parse(User.FindFirstValue("sub")!);
        var result = await businessProfileService.ReviewAsync(id, adminId, request.IsApproved, request.RejectionReason, ct);
        return Ok(ApiResponse<BusinessProfileResponse>.Ok(result,
            request.IsApproved ? "Business profile approved" : "Business profile rejected"));
    }
}
