using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace EBayClone.Infrastructure.Services;

public class AuctionEndingBackgroundService(
    IServiceScopeFactory scopeFactory,
    ILogger<AuctionEndingBackgroundService> logger) : BackgroundService
{
    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AuctionEndingBackgroundService started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessExpiredAuctionsAsync(stoppingToken);
            }
            catch (Exception ex) when (ex is not OperationCanceledException)
            {
                logger.LogError(ex, "Error processing expired auctions");
            }

            await Task.Delay(TimeSpan.FromSeconds(10), stoppingToken);
        }
    }

    private async Task ProcessExpiredAuctionsAsync(CancellationToken ct)
    {
        using var scope = scopeFactory.CreateScope();
        var listingRepo = scope.ServiceProvider.GetRequiredService<IRepository<Listing>>();
        var auctionService = scope.ServiceProvider.GetRequiredService<IAuctionService>();

        var expiredListingIds = await listingRepo.Query()
            .Where(l => l.ListingType == ListingType.Auction
                     && l.Status == ListingStatus.Active
                     && l.AuctionEndAt.HasValue
                     && l.AuctionEndAt <= DateTime.UtcNow)
            .Select(l => l.Id)
            .ToListAsync(ct);

        foreach (var id in expiredListingIds)
        {
            try
            {
                await auctionService.FinalizeAuctionAsync(id, ct);
                logger.LogInformation("Finalized auction: {ListingId}", id);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to finalize auction {ListingId}", id);
            }
        }
    }
}
