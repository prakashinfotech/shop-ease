using EBayClone.Application.DTOs.Auctions;

namespace EBayClone.Application.Interfaces;

public interface IAuctionNotifier
{
    Task BidPlacedAsync(Guid listingId, AuctionBidPlacedEvent evt, CancellationToken ct = default);
    Task AuctionEndedAsync(Guid listingId, Guid? winnerId, string? winnerHandle, decimal? winningAmount, bool reserveMet, CancellationToken ct = default);
    Task TimeExtendedAsync(Guid listingId, DateTime newEndAt, CancellationToken ct = default);
    Task AuctionCancelledAsync(Guid listingId, string reason, CancellationToken ct = default);
}
