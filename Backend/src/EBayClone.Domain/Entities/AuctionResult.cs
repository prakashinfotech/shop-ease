using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class AuctionResult : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public Guid? WinnerId { get; set; }
    public User? Winner { get; set; }

    public decimal? WinningAmount { get; set; }
    public bool ReserveMet { get; set; }
    public DateTime EndedAt { get; set; }
    public AuctionEndReason EndReason { get; set; }
}
