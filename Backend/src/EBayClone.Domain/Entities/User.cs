using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class User : BaseEntity
{
    public string FirstName { get; set; } = string.Empty;
    public string LastName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public AccountType AccountType { get; set; } = AccountType.Personal;
    public UserRole Role { get; set; } = UserRole.User;
    public bool IsEmailVerified { get; set; } = false;
    public string? PhoneNumber { get; set; }
    public string? AvatarUrl { get; set; }

    public bool IsSuspended { get; set; } = false;

    // Email verification
    public string? EmailVerificationToken { get; set; }
    public DateTime? EmailVerificationTokenExpiry { get; set; }

    // Password reset
    public string? PasswordResetToken { get; set; }
    public DateTime? PasswordResetTokenExpiry { get; set; }

    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
    public ICollection<ListingView> ListingViews { get; set; } = new List<ListingView>();
    public ICollection<Order> BuyerOrders { get; set; } = new List<Order>();
    public ICollection<RefreshToken> RefreshTokens { get; set; } = new List<RefreshToken>();
    public Cart? Cart { get; set; }
    public BusinessProfile? BusinessProfile { get; set; }
}
