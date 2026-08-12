using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class ListingView : BaseEntity
{
    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public DateTime LastViewedAt { get; set; } = DateTime.UtcNow;
}
