using EBayClone.Application.DTOs.Auctions;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.SignalR;

namespace EBayClone.API.Hubs;

public class SignalRAuctionNotifier(IHubContext<AuctionHub> hub) : IAuctionNotifier
{
    public Task BidPlacedAsync(Guid listingId, AuctionBidPlacedEvent evt, CancellationToken ct = default) =>
        hub.Clients.Group($"auction:{listingId}").SendAsync("bid-placed", evt, ct);

    public Task AuctionEndedAsync(Guid listingId, Guid? winnerId, string? winnerHandle, decimal? winningAmount, bool reserveMet, CancellationToken ct = default) =>
        hub.Clients.Group($"auction:{listingId}").SendAsync("auction-ended", new
        {
            listingId,
            winnerHandle,
            winningAmount,
            reserveMet,
        }, ct);

    public Task TimeExtendedAsync(Guid listingId, DateTime newEndAt, CancellationToken ct = default) =>
        hub.Clients.Group($"auction:{listingId}").SendAsync("time-extended", new { listingId, newEndAt }, ct);

    public Task AuctionCancelledAsync(Guid listingId, string reason, CancellationToken ct = default) =>
        hub.Clients.Group($"auction:{listingId}").SendAsync("auction-cancelled", new { listingId, reason }, ct);
}
