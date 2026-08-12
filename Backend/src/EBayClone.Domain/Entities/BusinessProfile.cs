using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class BusinessProfile : BaseEntity
{
    public string CompanyName { get; set; } = string.Empty;
    public string GstNumber { get; set; } = string.Empty;
    public string PanNumber { get; set; } = string.Empty;
    public string? BusinessAddress { get; set; }
    public string? BusinessPhone { get; set; }
    public string? BusinessEmail { get; set; }
    public string? BusinessWebsite { get; set; }
    public VerificationStatus VerificationStatus { get; set; } = VerificationStatus.Pending;
    public string? RejectionReason { get; set; }
    public DateTime? VerifiedAt { get; set; }
    public Guid? VerifiedByAdminId { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;
    public ICollection<UserDocument> Documents { get; set; } = new List<UserDocument>();
}
