using System.Text.Json;
using EBayClone.Application.DTOs.Listings;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class ListingApprovalService(
    IRepository<Listing> listingRepository,
    IRepository<ListingVersion> versionRepository,
    IRepository<ListingApprovalLog> logRepository,
    IRepository<ListingImage> imageRepository,
    IRepository<ListingAttributeValue> attributeValueRepository,
    IRepository<User> userRepository,
    IListingService listingService,
    IBackgroundEmailService backgroundEmailService,
    IConfiguration configuration,
    ILogger<ListingApprovalService> logger) : IListingApprovalService
{
    private readonly string _frontendUrl =
        configuration["AppSettings:FrontendUrl"]
        ?? throw new InvalidOperationException("AppSettings:FrontendUrl is not configured.");

    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static void ValidateSellerCanSell(User seller)
    {
        if (!seller.IsEmailVerified)
            throw new InvalidOperationException("Please verify your email before selling items.");

        if (seller.AccountType == AccountType.Business && seller.BusinessProfile is null)
            throw new InvalidOperationException("Please submit your business details before selling items.");
    }

    // ── Submit Draft/Rejected for approval ──────────────────────────────

    public async Task<ListingResponse> SubmitForApprovalAsync(Guid listingId, Guid sellerId, CancellationToken ct = default)
    {
        var listing = await LoadListingAsync(listingId, ct)
            ?? throw new KeyNotFoundException($"Listing {listingId} not found.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only submit your own listings.");

        var seller = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == sellerId, ct)
            ?? throw new KeyNotFoundException("Seller not found.");

        ValidateSellerCanSell(seller);

        if (listing.Status is not (ListingStatus.Draft or ListingStatus.Rejected))
            throw new InvalidOperationException($"Only Draft or Rejected listings can be submitted. Current status: {listing.Status}.");

        if (listing.HasPendingVersion)
            throw new InvalidOperationException("This listing already has a pending review.");

        var nextVersion = await NextVersionNumberAsync(listingId, ct);
        var snapshot = BuildSnapshot(listing);

        var version = new ListingVersion
        {
            ListingId = listingId,
            VersionNumber = nextVersion,
            SnapshotJson = JsonSerializer.Serialize(snapshot, JsonOpts),
            IsPendingUpdate = false,
            Status = ListingVersionStatus.PendingApproval,
            SubmittedAt = DateTime.UtcNow,
        };

        await versionRepository.AddAsync(version, ct);

        listing.Status = ListingStatus.PendingApproval;
        listing.UpdatedAt = DateTime.UtcNow;
        listingRepository.Update(listing);

        await listingRepository.SaveChangesAsync(ct);

        await LogActionAsync(listingId, version.Id, sellerId, ApprovalAction.Submitted, null, ct);

        var sellerEmail = seller.Email;
        var sellerName = seller.FirstName;
        var title = listing.Title;

        // Send email in background using scoped service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendListingPendingApprovalAsync(sellerEmail, sellerName, title, CancellationToken.None));

        logger.LogInformation("Listing {Id} submitted for approval by seller {SellerId}", listingId, sellerId);

        return await listingService.GetByIdAsync(listingId, ct);
    }

    // ── Submit update to an Active listing ─────────────────────────────

    public async Task SubmitUpdateForApprovalAsync(
        Guid listingId, Guid sellerId, SubmitListingUpdateRequest request, CancellationToken ct = default)
    {
        var listing = await LoadListingAsync(listingId, ct)
            ?? throw new KeyNotFoundException($"Listing {listingId} not found.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only submit your own listings.");

        var seller = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == sellerId, ct)
            ?? throw new KeyNotFoundException("Seller not found.");

        ValidateSellerCanSell(seller);

        if (listing.Status != ListingStatus.Active)
            throw new InvalidOperationException("Only Active listings support pending updates. Use 'submit' for Draft/Rejected listings.");

        if (listing.HasPendingVersion)
            throw new InvalidOperationException("This listing already has a pending update under review.");

        var nextVersion = await NextVersionNumberAsync(listingId, ct);
        var snapshotData = new ListingSnapshotData(
            request.Title,
            request.Description,
            request.ListingType,
            request.Price,
            request.DiscountAmount,
            request.StartingBid,
            request.ReservePrice,
            request.BuyItNowPrice,
            request.AuctionStartAt,
            request.AuctionEndAt,
            request.Quantity,
            request.FreeShipping,
            request.CategoryId,
            (request.AttributeValues ?? []).Select(a => new AttributeEntry(a.CategoryAttributeId, a.Value ?? "")).ToList(),
            (request.Images ?? []).Select(i => new ImageEntry(i.Url, i.AltText, i.SortOrder)).ToList()
        );

        var version = new ListingVersion
        {
            ListingId = listingId,
            VersionNumber = nextVersion,
            SnapshotJson = JsonSerializer.Serialize(snapshotData, JsonOpts),
            IsPendingUpdate = true,
            Status = ListingVersionStatus.PendingApproval,
            SubmittedAt = DateTime.UtcNow,
        };

        await versionRepository.AddAsync(version, ct);

        listing.HasPendingVersion = true;
        listing.UpdatedAt = DateTime.UtcNow;
        listingRepository.Update(listing);

        await listingRepository.SaveChangesAsync(ct);

        await LogActionAsync(listingId, version.Id, sellerId, ApprovalAction.Submitted, null, ct);

        var sellerEmail = seller.Email;
        var sellerName = seller.FirstName;
        var title = listing.Title;

        // Send email in background using scoped service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendListingPendingApprovalAsync(sellerEmail, sellerName, title, CancellationToken.None));

        logger.LogInformation("Listing update {Id} submitted for approval by seller {SellerId}", listingId, sellerId);
    }

    // ── Admin: Approve ──────────────────────────────────────────────────

    public async Task<ListingResponse> ApproveAsync(Guid listingId, Guid adminId, string? notes, CancellationToken ct = default)
    {
        var listing = await LoadListingAsync(listingId, ct)
            ?? throw new KeyNotFoundException($"Listing {listingId} not found.");

        var pendingVersion = await GetPendingVersionEntityAsync(listingId, ct);

        if (pendingVersion is null)
            throw new InvalidOperationException("There is no pending version available for approval.");

        if (listing.Status == ListingStatus.PendingApproval && !pendingVersion.IsPendingUpdate)
        {
            // First-time listing approval: activate listing and approve version.
            listing.Status = ListingStatus.Active;
            pendingVersion.Status = ListingVersionStatus.Approved;
        }
        else if (listing.HasPendingVersion && pendingVersion.IsPendingUpdate)
        {
            // Pending update approval: apply snapshot and clear pending flag.
            await ApplySnapshotToListingAsync(listing, pendingVersion.SnapshotJson, ct);
            listing.HasPendingVersion = false;
            pendingVersion.Status = ListingVersionStatus.Approved;
        }
        else
        {
            throw new InvalidOperationException("Listing cannot be approved in the current state.");
        }

        pendingVersion.ReviewedByAdminId = adminId;
        pendingVersion.ReviewedAt = DateTime.UtcNow;
        listing.UpdatedAt = DateTime.UtcNow;

        listingRepository.Update(listing);
        versionRepository.Update(pendingVersion);
        await listingRepository.SaveChangesAsync(ct);

        await LogActionAsync(listingId, pendingVersion.Id, adminId, ApprovalAction.Approved, notes, ct);

        var seller = await userRepository.GetByIdAsync(listing.SellerId, ct);
        if (seller is not null)
        {
            var listingUrl = $"{_frontendUrl}/listings/{listingId}";
            var sellerEmail = seller.Email;
            var sellerName = seller.FirstName;
            var title = listing.Title;

            // Send email in background using scoped service
            backgroundEmailService.EnqueueEmail(s => 
                s.SendListingApprovedAsync(sellerEmail, sellerName, title, listingUrl, CancellationToken.None));
        }

        logger.LogInformation("Listing {Id} approved by admin {AdminId}", listingId, adminId);

        return await listingService.GetByIdAsync(listingId, ct);
    }

    // ── Admin: Reject ───────────────────────────────────────────────────

    public async Task<ListingResponse> RejectAsync(Guid listingId, Guid adminId, string reason, string? notes, CancellationToken ct = default)
    {
        var listing = await LoadListingAsync(listingId, ct)
            ?? throw new KeyNotFoundException($"Listing {listingId} not found.");

        var pendingVersion = await GetPendingVersionEntityAsync(listingId, ct);

        if (listing.Status == ListingStatus.PendingApproval && pendingVersion is not null && !pendingVersion.IsPendingUpdate)
        {
            // New listing rejection
            listing.Status = ListingStatus.Rejected;
            pendingVersion.Status = ListingVersionStatus.Rejected;
            pendingVersion.RejectionReason = reason;
            pendingVersion.ReviewedByAdminId = adminId;
            pendingVersion.ReviewedAt = DateTime.UtcNow;
        }
        else if (listing.HasPendingVersion && pendingVersion is not null && pendingVersion.IsPendingUpdate)
        {
            // Pending update rejection: discard the version, listing stays Active
            listing.HasPendingVersion = false;
            pendingVersion.Status = ListingVersionStatus.Rejected;
            pendingVersion.RejectionReason = reason;
            pendingVersion.ReviewedByAdminId = adminId;
            pendingVersion.ReviewedAt = DateTime.UtcNow;
        }
        else
        {
            throw new InvalidOperationException("This listing has no pending approval or update to reject.");
        }

        listing.UpdatedAt = DateTime.UtcNow;
        listingRepository.Update(listing);
        versionRepository.Update(pendingVersion);
        await listingRepository.SaveChangesAsync(ct);

        await LogActionAsync(listingId, pendingVersion.Id, adminId, ApprovalAction.Rejected, notes ?? reason, ct);

        var seller = await userRepository.GetByIdAsync(listing.SellerId, ct);
        if (seller is not null)
        {
            var sellerEmail = seller.Email;
            var sellerName = seller.FirstName;
            var title = listing.Title;

            // Send email in background using scoped service
            backgroundEmailService.EnqueueEmail(s => 
                s.SendListingRejectedAsync(sellerEmail, sellerName, title, reason, CancellationToken.None));
        }

        logger.LogInformation("Listing {Id} rejected by admin {AdminId}", listingId, adminId);

        return await listingService.GetByIdAsync(listingId, ct);
    }

    // ── Version queries ─────────────────────────────────────────────────

    public async Task<IReadOnlyList<ListingVersionResponse>> GetVersionsAsync(Guid listingId, CancellationToken ct = default)
    {
        var versions = await versionRepository.Query()
            .Include(v => v.ReviewedByAdmin)
            .AsNoTracking()
            .Where(v => v.ListingId == listingId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync(ct);

        return versions.Select(MapVersionToResponse).ToList();
    }

    public async Task<ListingVersionResponse?> GetPendingVersionAsync(Guid listingId, CancellationToken ct = default)
    {
        var version = await versionRepository.Query()
            .Include(v => v.ReviewedByAdmin)
            .AsNoTracking()
            .FirstOrDefaultAsync(v => v.ListingId == listingId && v.Status == ListingVersionStatus.PendingApproval, ct);

        return version is null ? null : MapVersionToResponse(version);
    }

    // ── Helpers ─────────────────────────────────────────────────────────

    private Task<Listing?> LoadListingAsync(Guid id, CancellationToken ct)
        => listingRepository.Query()
            .Include(l => l.Images)
            .Include(l => l.AttributeValues)
            .FirstOrDefaultAsync(l => l.Id == id, ct);

    private Task<ListingVersion?> GetPendingVersionEntityAsync(Guid listingId, CancellationToken ct)
        => versionRepository.Query()
            .FirstOrDefaultAsync(v => v.ListingId == listingId && v.Status == ListingVersionStatus.PendingApproval, ct);

    private async Task<int> NextVersionNumberAsync(Guid listingId, CancellationToken ct)
    {
        var max = await versionRepository.Query()
            .IgnoreQueryFilters()
            .Where(v => v.ListingId == listingId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(ct);
        return (max ?? 0) + 1;
    }

    private async Task LogActionAsync(
        Guid listingId, Guid versionId, Guid actorId, ApprovalAction action, string? notes, CancellationToken ct)
    {
        var log = new ListingApprovalLog
        {
            ListingId = listingId,
            VersionId = versionId,
            AdminId = actorId,
            Action = action,
            Notes = notes,
        };
        await logRepository.AddAsync(log, ct);
        await logRepository.SaveChangesAsync(ct);
    }

    private async Task ApplySnapshotToListingAsync(Listing listing, string snapshotJson, CancellationToken ct)
    {
        var data = JsonSerializer.Deserialize<ListingSnapshotData>(snapshotJson, JsonOpts);
        if (data is null) return;

        listing.Title = data.Title;
        listing.Description = data.Description;
        listing.ListingType = (ListingType)data.ListingType;
        listing.Price = data.Price;
        listing.DiscountAmount = data.DiscountAmount;
        listing.StartingBid = data.StartingBid;
        listing.ReservePrice = data.ReservePrice;
        listing.BuyItNowPrice = data.BuyItNowPrice;
        listing.AuctionStartAt = data.AuctionStartAt;
        listing.AuctionEndAt = data.AuctionEndAt;
        listing.Quantity = data.Quantity;
        listing.FreeShipping = data.FreeShipping;
        listing.CategoryId = data.CategoryId;

        // Replace attribute values
        foreach (var av in listing.AttributeValues.Where(v => !v.IsDeleted))
        {
            av.IsDeleted = true;
            av.DeletedAt = DateTime.UtcNow;
        }
        foreach (var entry in data.Attributes)
        {
            await attributeValueRepository.AddAsync(new ListingAttributeValue
            {
                ListingId = listing.Id,
                CategoryAttributeId = entry.CategoryAttributeId,
                Value = entry.Value,
            }, ct);
        }

        // Replace images
        foreach (var img in listing.Images.Where(i => !i.IsDeleted))
        {
            img.IsDeleted = true;
            img.DeletedAt = DateTime.UtcNow;
        }
        foreach (var entry in data.Images)
        {
            await imageRepository.AddAsync(new ListingImage
            {
                ListingId = listing.Id,
                Url = entry.Url,
                AltText = entry.AltText,
                SortOrder = entry.SortOrder,
            }, ct);
        }

        listing.PrimaryImageUrl = data.Images.OrderBy(i => i.SortOrder).Select(i => i.Url).FirstOrDefault();
    }

    private static ListingSnapshotData BuildSnapshot(Listing l) => new(
        l.Title, l.Description, (int)l.ListingType, l.Price, l.DiscountAmount,
        l.StartingBid, l.ReservePrice, l.BuyItNowPrice,
        l.AuctionStartAt, l.AuctionEndAt, l.Quantity, l.FreeShipping, l.CategoryId,
        l.AttributeValues.Where(v => !v.IsDeleted).Select(v => new AttributeEntry(v.CategoryAttributeId, v.Value)).ToList(),
        l.Images.Where(i => !i.IsDeleted).OrderBy(i => i.SortOrder).Select(i => new ImageEntry(i.Url, i.AltText, i.SortOrder)).ToList()
    );

    private static ListingVersionResponse MapVersionToResponse(ListingVersion v)
    {
        ListingVersionSnapshotDto? snapshot = null;
        if (!string.IsNullOrEmpty(v.SnapshotJson))
        {
            try
            {
                var data = JsonSerializer.Deserialize<ListingSnapshotData>(v.SnapshotJson, JsonOpts);
                if (data is not null)
                    snapshot = new ListingVersionSnapshotDto(
                        data.Title, data.Description, data.ListingType,
                        data.Price, data.DiscountAmount, data.StartingBid, data.BuyItNowPrice,
                        data.Quantity, data.FreeShipping, null,
                        data.Images.Select(i => i.Url).ToList());
            }
            catch { /* snapshot is informational – don't throw */ }
        }

        return new ListingVersionResponse(
            v.Id, v.ListingId, v.VersionNumber, v.IsPendingUpdate,
            (int)v.Status, v.Status.ToString(),
            v.RejectionReason,
            v.ReviewedByAdmin is null ? null : $"{v.ReviewedByAdmin.FirstName} {v.ReviewedByAdmin.LastName}",
            v.ReviewedAt, v.SubmittedAt, snapshot);
    }
}

// ── Internal snapshot model (serialised to ListingVersion.SnapshotJson) ──

internal record ListingSnapshotData(
    string Title, string Description, int ListingType,
    decimal Price, decimal DiscountAmount,
    decimal? StartingBid, decimal? ReservePrice, decimal? BuyItNowPrice,
    DateTime? AuctionStartAt, DateTime? AuctionEndAt,
    int Quantity, bool FreeShipping, Guid? CategoryId,
    IReadOnlyList<AttributeEntry> Attributes,
    IReadOnlyList<ImageEntry> Images
);

internal record AttributeEntry(Guid CategoryAttributeId, string Value);
internal record ImageEntry(string Url, string? AltText, int SortOrder);
