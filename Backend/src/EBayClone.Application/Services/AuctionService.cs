using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Auctions;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using System.Security.Cryptography;
using System.Text;

namespace EBayClone.Application.Services;

public class AuctionService(
    IRepository<Listing> listingRepo,
    IRepository<Bid> bidRepo,
    IRepository<AuctionResult> resultRepo,
    IRepository<Order> orderRepo,
    IAuctionNotifier notifier,
    AuctionBidLockService lockService,
    ILogger<AuctionService> logger) : IAuctionService
{
    private const string ListingNotFound = "Listing not found.";

    public async Task<BidResponse> PlaceBidAsync(Guid listingId, Guid bidderId, decimal amount, CancellationToken ct = default)
    {
        var sem = lockService.GetLock(listingId);
        await sem.WaitAsync(ct);
        try
        {
            var listing = await listingRepo.Query()
                .FirstOrDefaultAsync(l => l.Id == listingId, ct)
                ?? throw new KeyNotFoundException(ListingNotFound);

            if (listing.ListingType != ListingType.Auction)
                throw new InvalidOperationException("This listing is not an auction.");
            if (listing.Status != ListingStatus.Active)
                throw new InvalidOperationException("Auction is not active.");
            if (listing.AuctionEndAt.HasValue && listing.AuctionEndAt <= DateTime.UtcNow)
                throw new InvalidOperationException("Auction has ended.");
            if (listing.SellerId == bidderId)
                throw new InvalidOperationException("Sellers cannot bid on their own listings.");

            if (listing.CurrentBidderId == bidderId)
                throw new InvalidOperationException("You are already the highest bidder. Wait for another bidder before placing a higher bid.");

            var minRequired = listing.CurrentBidAmount.HasValue
                ? listing.CurrentBidAmount.Value + listing.MinBidIncrement
                : listing.StartingBid ?? listing.MinBidIncrement;

            if (amount < minRequired)
                throw new InvalidOperationException($"Bid must be at least {minRequired:F2}.");

            // Buy It Now — bid meets or exceeds BIN price → close auction immediately
            if (listing.BuyItNowPrice.HasValue && amount >= listing.BuyItNowPrice.Value)
            {
                var binBid = new Bid
                {
                    ListingId = listingId,
                    BidderId = bidderId,
                    Amount = listing.BuyItNowPrice.Value,
                    IsWinning = true,
                };
                await bidRepo.AddAsync(binBid, ct);

                // Clear any previous winning bid
                if (listing.CurrentBidderId.HasValue && listing.CurrentBidderId != bidderId)
                {
                    var prevBid = await bidRepo.Query()
                        .Where(b => b.ListingId == listingId && b.BidderId == listing.CurrentBidderId && b.IsWinning)
                        .OrderByDescending(b => b.CreatedAt)
                        .FirstOrDefaultAsync(ct);
                    if (prevBid != null) { prevBid.IsWinning = false; bidRepo.Update(prevBid); }
                }

                listing.CurrentBidAmount = listing.BuyItNowPrice.Value;
                listing.CurrentBidderId = bidderId;
                listing.BidCount++;
                listing.Status = ListingStatus.Sold;
                listingRepo.Update(listing);

                var order = new Order
                {
                    OrderNumber = $"BIN-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                    BuyerId = bidderId,
                    Status = OrderStatus.Confirmed,
                    PaymentMethod = "Auction",
                    PaymentStatus = PaymentStatus.Pending,
                    ShippingAddress = "Pending — buyer to provide",
                    TotalAmount = listing.BuyItNowPrice.Value,
                };
                order.Items.Add(new OrderItem
                {
                    ListingId = listingId,
                    ListingTitle = listing.Title,
                    Quantity = 1,
                    UnitPrice = listing.BuyItNowPrice.Value,
                });
                await orderRepo.AddAsync(order, ct);

                await resultRepo.AddAsync(new AuctionResult
                {
                    ListingId = listingId,
                    WinnerId = bidderId,
                    WinningAmount = listing.BuyItNowPrice.Value,
                    ReserveMet = true,
                    EndedAt = DateTime.UtcNow,
                    EndReason = AuctionEndReason.BuyItNow,
                }, ct);

                await listingRepo.SaveChangesAsync(ct);

                await notifier.AuctionEndedAsync(listingId, bidderId, MaskBidderId(bidderId),
                    listing.BuyItNowPrice.Value, true, ct);

                logger.LogInformation("[BIN] Buy It Now triggered: listing={ListingId} buyer={BidderId} price={Price}",
                    listingId, bidderId, listing.BuyItNowPrice.Value);

                return new BidResponse(binBid.Id, listingId, MaskBidderId(bidderId),
                    listing.BuyItNowPrice.Value, true, binBid.CreatedAt);
            }

            var previousWinnerId = listing.CurrentBidderId;

            if (previousWinnerId.HasValue && previousWinnerId != bidderId)
            {
                var prevBid = await bidRepo.Query()
                    .Where(b => b.ListingId == listingId && b.BidderId == previousWinnerId && b.IsWinning)
                    .OrderByDescending(b => b.CreatedAt)
                    .FirstOrDefaultAsync(ct);

                if (prevBid != null)
                {
                    prevBid.IsWinning = false;
                    bidRepo.Update(prevBid);
                }
            }

            var bid = new Bid
            {
                ListingId = listingId,
                BidderId = bidderId,
                Amount = amount,
                IsWinning = true,
            };
            await bidRepo.AddAsync(bid, ct);

            listing.CurrentBidAmount = amount;
            listing.CurrentBidderId = bidderId;
            listing.BidCount++;

            if (listing.AutoExtendOnBid && listing.AuctionEndAt.HasValue &&
                (listing.AuctionEndAt.Value - DateTime.UtcNow).TotalMinutes < 2)
            {
                listing.AuctionEndAt = listing.AuctionEndAt.Value.AddMinutes(2);
                await notifier.TimeExtendedAsync(listingId, listing.AuctionEndAt.Value, ct);
            }

            listingRepo.Update(listing);
            await listingRepo.SaveChangesAsync(ct);

            var bidderHandle = MaskBidderId(bidderId);
            var evt = new AuctionBidPlacedEvent(listingId, amount, listing.BidCount, listing.AuctionEndAt, bidderHandle);
            await notifier.BidPlacedAsync(listingId, evt, ct);

            if (previousWinnerId.HasValue && previousWinnerId != bidderId)
                logger.LogInformation("[EMAIL] Outbid | To: {UserId} | Listing: {ListingId}", previousWinnerId, listingId);

            logger.LogInformation("Bid placed: listing={ListingId} bidder={BidderId} amount={Amount}", listingId, bidderId, amount);

            return new BidResponse(bid.Id, listingId, bidderHandle, amount, true, bid.CreatedAt);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<PagedResult<BidResponse>> GetBidHistoryAsync(Guid listingId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = bidRepo.Query()
            .Where(b => b.ListingId == listingId)
            .OrderByDescending(b => b.Amount)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<BidResponse>.Create(
            items.Select(b => new BidResponse(b.Id, listingId, MaskBidderId(b.BidderId), b.Amount, b.IsWinning, b.CreatedAt)).ToList(),
            total, page, pageSize);
    }

    public async Task<AuctionStatusResponse> GetAuctionStatusAsync(Guid listingId, Guid? viewerId = null, CancellationToken ct = default)
    {
        var listing = await listingRepo.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == listingId, ct)
            ?? throw new KeyNotFoundException(ListingNotFound);

        var reserveMet = listing.ReservePrice is null ||
            (listing.CurrentBidAmount ?? 0) >= listing.ReservePrice;

        var isCurrentUserWinning = viewerId.HasValue && listing.CurrentBidderId == viewerId;

        return MapToStatus(listing, reserveMet, isCurrentUserWinning);
    }

    public async Task<PagedResult<AuctionStatusResponse>> GetActiveAuctionsAsync(int page, int pageSize, CancellationToken ct = default)
    {
        var q = listingRepo.Query()
            .Where(l => l.ListingType == ListingType.Auction && l.Status == ListingStatus.Active)
            .OrderByDescending(l => l.AuctionEndAt)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<AuctionStatusResponse>.Create(
            items.Select(l => MapToStatus(l, l.ReservePrice is null || (l.CurrentBidAmount ?? 0) >= l.ReservePrice)).ToList(),
            total, page, pageSize);
    }

    public async Task<PagedResult<BidResponse>> GetMyBidsAsync(Guid userId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = bidRepo.Query()
            .Where(b => b.BidderId == userId)
            .OrderByDescending(b => b.CreatedAt)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<BidResponse>.Create(
            items.Select(b => new BidResponse(b.Id, b.ListingId, MaskBidderId(b.BidderId), b.Amount, b.IsWinning, b.CreatedAt)).ToList(),
            total, page, pageSize);
    }

    public async Task FinalizeAuctionAsync(Guid listingId, CancellationToken ct = default)
    {
        var sem = lockService.GetLock(listingId);
        await sem.WaitAsync(ct);
        try
        {
            var listing = await listingRepo.Query()
                .FirstOrDefaultAsync(l => l.Id == listingId, ct);

            if (listing is null || listing.Status != ListingStatus.Active) return;

            var winningBid = await bidRepo.Query()
                .Include(b => b.Bidder)
                .Where(b => b.ListingId == listingId && b.IsWinning)
                .OrderByDescending(b => b.Amount)
                .FirstOrDefaultAsync(ct);

            var reserveMet = listing.ReservePrice is null ||
                (winningBid?.Amount ?? 0) >= listing.ReservePrice;

            if (winningBid is null || !reserveMet)
            {
                listing.Status = ListingStatus.Ended;
                listingRepo.Update(listing);

                await resultRepo.AddAsync(new AuctionResult
                {
                    ListingId = listingId,
                    ReserveMet = false,
                    EndedAt = DateTime.UtcNow,
                    EndReason = AuctionEndReason.TimeExpired,
                }, ct);
                await resultRepo.SaveChangesAsync(ct);

                await notifier.AuctionEndedAsync(listingId, null, null, null, false, ct);
                logger.LogInformation("[EMAIL] Auction Ended - No Winner | Listing: {ListingId}", listingId);
                return;
            }

            var order = new Order
            {
                OrderNumber = $"AUC-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString()[..6].ToUpper()}",
                BuyerId = winningBid.BidderId,
                Status = OrderStatus.Confirmed,
                PaymentMethod = "Auction",
                PaymentStatus = PaymentStatus.Pending,
                ShippingAddress = "Pending — buyer to provide",
                TotalAmount = winningBid.Amount,
            };
            order.Items.Add(new OrderItem
            {
                ListingId = listingId,
                ListingTitle = listing.Title,
                Quantity = 1,
                UnitPrice = winningBid.Amount,
            });
            await orderRepo.AddAsync(order, ct);

            listing.Status = ListingStatus.Sold;
            listingRepo.Update(listing);

            await resultRepo.AddAsync(new AuctionResult
            {
                ListingId = listingId,
                WinnerId = winningBid.BidderId,
                WinningAmount = winningBid.Amount,
                ReserveMet = true,
                EndedAt = DateTime.UtcNow,
                EndReason = AuctionEndReason.TimeExpired,
            }, ct);
            await resultRepo.SaveChangesAsync(ct);

            await notifier.AuctionEndedAsync(listingId, winningBid.BidderId, MaskBidderId(winningBid.BidderId), winningBid.Amount, true, ct);
            logger.LogInformation("[EMAIL] Auction Won | To: {Email} | Listing: {ListingId} | Amount: {Amount}",
                winningBid.Bidder.Email, listingId, winningBid.Amount);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task CancelAuctionAsync(Guid listingId, CancellationToken ct = default)
    {
        var sem = lockService.GetLock(listingId);
        await sem.WaitAsync(ct);
        try
        {
            var listing = await listingRepo.Query()
                .FirstOrDefaultAsync(l => l.Id == listingId, ct)
                ?? throw new KeyNotFoundException(ListingNotFound);

            if (listing.ListingType != ListingType.Auction)
                throw new InvalidOperationException("Not an auction listing.");
            if (listing.Status != ListingStatus.Active)
                throw new InvalidOperationException("Only active auctions can be cancelled.");

            listing.Status = ListingStatus.Ended;
            listingRepo.Update(listing);

            await resultRepo.AddAsync(new AuctionResult
            {
                ListingId = listingId,
                ReserveMet = false,
                EndedAt = DateTime.UtcNow,
                EndReason = AuctionEndReason.AdminCancelled,
            }, ct);
            await resultRepo.SaveChangesAsync(ct);

            await notifier.AuctionCancelledAsync(listingId, "Auction cancelled by administrator.", ct);
            logger.LogInformation("Auction cancelled by admin: {ListingId}", listingId);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task ExtendAuctionAsync(Guid listingId, int minutes, CancellationToken ct = default)
    {
        if (minutes <= 0 || minutes > 1440)
            throw new InvalidOperationException("Extension must be between 1 and 1440 minutes.");

        var sem = lockService.GetLock(listingId);
        await sem.WaitAsync(ct);
        try
        {
            var listing = await listingRepo.Query()
                .FirstOrDefaultAsync(l => l.Id == listingId, ct)
                ?? throw new KeyNotFoundException(ListingNotFound);

            if (listing.ListingType != ListingType.Auction)
                throw new InvalidOperationException("Not an auction listing.");
            if (listing.Status != ListingStatus.Active)
                throw new InvalidOperationException("Only active auctions can be extended.");

            listing.AuctionEndAt = (listing.AuctionEndAt ?? DateTime.UtcNow).AddMinutes(minutes);
            listingRepo.Update(listing);
            await listingRepo.SaveChangesAsync(ct);

            await notifier.TimeExtendedAsync(listingId, listing.AuctionEndAt.Value, ct);
            logger.LogInformation("Auction {ListingId} extended by {Minutes} min", listingId, minutes);
        }
        finally
        {
            sem.Release();
        }
    }

    public async Task<PagedResult<AdminAuctionResponse>> GetAdminAuctionsAsync(AdminAuctionsQuery query, CancellationToken ct = default)
    {
        var q = listingRepo.Query()
            .Include(l => l.Seller)
            .Where(l => l.ListingType == ListingType.Auction)
            .AsNoTracking();

        if (query.Status.HasValue)
            q = q.Where(l => (int)l.Status == query.Status.Value);

        q = q.OrderByDescending(l => l.AuctionEndAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((query.Page - 1) * query.PageSize).Take(query.PageSize).ToListAsync(ct);

        return PagedResult<AdminAuctionResponse>.Create(
            items.Select(l => new AdminAuctionResponse(
                l.Id, l.Title, l.StartingBid, l.CurrentBidAmount, l.ReservePrice,
                l.BidCount, l.AuctionEndAt, (int)l.Status,
                $"{l.Seller?.FirstName} {l.Seller?.LastName}".Trim(), l.SellerId
            )).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<PagedResult<AdminBidResponse>> GetAdminBidHistoryAsync(Guid listingId, int page, int pageSize, CancellationToken ct = default)
    {
        var q = bidRepo.Query()
            .Include(b => b.Bidder)
            .Where(b => b.ListingId == listingId)
            .OrderByDescending(b => b.Amount)
            .AsNoTracking();

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<AdminBidResponse>.Create(
            items.Select(b => new AdminBidResponse(
                b.Id, listingId, b.BidderId,
                $"{b.Bidder?.FirstName} {b.Bidder?.LastName}".Trim(),
                b.Bidder?.Email ?? "",
                b.Amount, b.IsWinning, b.CreatedAt
            )).ToList(),
            total, page, pageSize);
    }

    private static string MaskBidderId(Guid userId)
    {
        var hash = Convert.ToHexString(MD5.HashData(
            Encoding.UTF8.GetBytes(userId.ToString())))[..6].ToLower();
        return $"buyer_{hash}";
    }

    private static AuctionStatusResponse MapToStatus(Listing l, bool reserveMet, bool isCurrentUserWinning = false) =>
        new(l.Id, l.Title, l.StartingBid, l.CurrentBidAmount, l.ReservePrice,
            reserveMet, l.BuyItNowPrice, l.MinBidIncrement,
            l.BidCount, l.AuctionStartAt, l.AuctionEndAt, (int)l.Status, isCurrentUserWinning);
}
