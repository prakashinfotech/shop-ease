using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;

namespace EBayClone.API.Hubs;

[Authorize]
public class AuctionHub : Hub
{
    public async Task JoinAuction(string listingId)
    {
        await Groups.AddToGroupAsync(Context.ConnectionId, $"auction:{listingId}");
    }

    public async Task LeaveAuction(string listingId)
    {
        await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"auction:{listingId}");
    }
}
