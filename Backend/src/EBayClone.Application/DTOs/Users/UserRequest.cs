namespace EBayClone.Application.DTOs.Users;

public record UpdateProfileRequest(
    string FirstName,
    string LastName,
    string Email,
    string? PhoneNumber = null
);