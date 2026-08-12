using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Auctions;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuctionsController(IAuctionService auctionService) : ControllerBase
{
    [HttpGet("active")]
    public async Task<ActionResult<ApiResponse<PagedResult<AuctionStatusResponse>>>> GetActive(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 12, CancellationToken ct = default)
    {
        var result = await auctionService.GetActiveAuctionsAsync(page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<AuctionStatusResponse>>.Ok(result));
    }

    [HttpGet("{listingId:guid}/status")]
    public async Task<ActionResult<ApiResponse<AuctionStatusResponse>>> GetStatus(Guid listingId, CancellationToken ct)
    {
        Guid? viewerId = User.FindFirstValue("sub") is string sub ? Guid.Parse(sub) : null;
        var result = await auctionService.GetAuctionStatusAsync(listingId, viewerId, ct);
        return Ok(ApiResponse<AuctionStatusResponse>.Ok(result));
    }

    [HttpGet("{listingId:guid}/bids")]
    public async Task<ActionResult<ApiResponse<PagedResult<BidResponse>>>> GetBids(
        Guid listingId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var result = await auctionService.GetBidHistoryAsync(listingId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<BidResponse>>.Ok(result));
    }

    [HttpPost("{listingId:guid}/bids")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<BidResponse>>> PlaceBid(
        Guid listingId, [FromBody] PlaceBidRequest request, CancellationToken ct)
    {
        var bidderId = GetUserId();
        var result = await auctionService.PlaceBidAsync(listingId, bidderId, request.Amount, ct);
        return Ok(ApiResponse<BidResponse>.Ok(result, "Bid placed successfully"));
    }

    [HttpGet("my-bids")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<PagedResult<BidResponse>>>> GetMyBids(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 20, CancellationToken ct = default)
    {
        var userId = GetUserId();
        var result = await auctionService.GetMyBidsAsync(userId, page, pageSize, ct);
        return Ok(ApiResponse<PagedResult<BidResponse>>.Ok(result));
    }

    private Guid GetUserId() => Guid.Parse(User.FindFirstValue("sub")!);
}
