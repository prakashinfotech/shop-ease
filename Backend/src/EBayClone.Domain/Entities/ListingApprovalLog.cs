using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class ListingApprovalLog : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public Guid? VersionId { get; set; }
    public ListingVersion? Version { get; set; }

    public Guid AdminId { get; set; }
    public User Admin { get; set; } = null!;

    public ApprovalAction Action { get; set; }
    public string? Notes { get; set; }
}
