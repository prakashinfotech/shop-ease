using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Auctions;

namespace EBayClone.Application.Interfaces;

public interface IAuctionService
{
    Task<BidResponse> PlaceBidAsync(Guid listingId, Guid bidderId, decimal amount, CancellationToken ct = default);
    Task<PagedResult<BidResponse>> GetBidHistoryAsync(Guid listingId, int page, int pageSize, CancellationToken ct = default);
    Task<AuctionStatusResponse> GetAuctionStatusAsync(Guid listingId, Guid? viewerId = null, CancellationToken ct = default);
    Task<PagedResult<AuctionStatusResponse>> GetActiveAuctionsAsync(int page, int pageSize, CancellationToken ct = default);
    Task<PagedResult<BidResponse>> GetMyBidsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default);
    Task FinalizeAuctionAsync(Guid listingId, CancellationToken ct = default);
    Task CancelAuctionAsync(Guid listingId, CancellationToken ct = default);
    Task ExtendAuctionAsync(Guid listingId, int minutes, CancellationToken ct = default);
    Task<PagedResult<AdminAuctionResponse>> GetAdminAuctionsAsync(AdminAuctionsQuery query, CancellationToken ct = default);
    Task<PagedResult<AdminBidResponse>> GetAdminBidHistoryAsync(Guid listingId, int page, int pageSize, CancellationToken ct = default);
}
