using EBayClone.Domain.Enums;

namespace EBayClone.Application.DTOs.Auth;

public record RegisterRequest(
    string FirstName,
    string LastName,
    string Email,
    string Password,
    AccountType AccountType = AccountType.Personal
);