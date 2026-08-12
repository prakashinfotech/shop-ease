using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class RefreshToken : BaseEntity
{
    public string Token { get; set; } = string.Empty;
    public DateTime ExpiresAt { get; set; }
    public bool IsRevoked { get; set; } = false;
    public string? ReplacedByToken { get; set; }
    public string? CreatedByIp { get; set; }

    public Guid UserId { get; set; }
    public User User { get; set; } = null!;

    public bool IsActive => !IsRevoked && DateTime.UtcNow < ExpiresAt;
}