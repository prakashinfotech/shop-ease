using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class ListingVersion : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public int VersionNumber { get; set; }

    /// <summary>
    /// JSON snapshot of the listing data submitted for approval.
    /// For pending updates to Active listings this holds the proposed changes.
    /// For new/rejected listings this holds the current submission state.
    /// </summary>
    public string SnapshotJson { get; set; } = string.Empty;

    /// <summary>True when this version represents a proposed edit to an already-Active listing.</summary>
    public bool IsPendingUpdate { get; set; } = false;

    public ListingVersionStatus Status { get; set; } = ListingVersionStatus.PendingApproval;
    public string? RejectionReason { get; set; }

    public Guid? ReviewedByAdminId { get; set; }
    public User? ReviewedByAdmin { get; set; }
    public DateTime? ReviewedAt { get; set; }
    public DateTime SubmittedAt { get; set; } = DateTime.UtcNow;
}
