namespace EBayClone.Application.DTOs.Users;

public record UserResponse(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber,
    string? AvatarUrl,
    int AccountType,
    string Role,
    bool IsEmailVerified,
    bool IsSuspended,
    bool IsDeleted,
    DateTime CreatedAt,
    string? BusinessVerificationStatus = null
);
