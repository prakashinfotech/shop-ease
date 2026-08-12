namespace EBayClone.Application.DTOs.Auth;

public record AuthResponse(
    UserDto User,
    string AccessToken,
    string RefreshToken
);

public record RefreshRequest(string RefreshToken);

public record UserDto(
    Guid Id,
    string FirstName,
    string LastName,
    string Email,
    string AccountType,
    string Role,
    bool IsEmailVerified,
    bool IsSuspended,
    string? BusinessVerificationStatus = null
);