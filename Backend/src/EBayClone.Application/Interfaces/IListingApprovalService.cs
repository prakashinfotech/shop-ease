using EBayClone.Application.DTOs.Listings;

namespace EBayClone.Application.Interfaces;

public interface IListingApprovalService
{
    /// <summary>Submit a Draft or Rejected listing for approval (no data change, just status).</summary>
    Task<ListingResponse> SubmitForApprovalAsync(Guid listingId, Guid sellerId, CancellationToken ct = default);

    /// <summary>
    /// Submit a proposed update to an Active listing.
    /// The listing remains Active while the update is held in a ListingVersion for review.
    /// </summary>
    Task SubmitUpdateForApprovalAsync(Guid listingId, Guid sellerId, SubmitListingUpdateRequest request, CancellationToken ct = default);

    /// <summary>Admin: approve a listing (PendingApproval → Active) or approve a pending update (apply snapshot).</summary>
    Task<ListingResponse> ApproveAsync(Guid listingId, Guid adminId, string? notes, CancellationToken ct = default);

    /// <summary>Admin: reject a listing or discard a pending update.</summary>
    Task<ListingResponse> RejectAsync(Guid listingId, Guid adminId, string reason, string? notes, CancellationToken ct = default);

    Task<IReadOnlyList<ListingVersionResponse>> GetVersionsAsync(Guid listingId, CancellationToken ct = default);
    Task<ListingVersionResponse?> GetPendingVersionAsync(Guid listingId, CancellationToken ct = default);
}
