using System.Security.Claims;
using EBayClone.Domain.Entities;

namespace EBayClone.Application.Interfaces;

public interface IJwtService
{
    string GenerateAccessToken(User user);
    string GenerateRefreshToken();
    ClaimsPrincipal? GetPrincipalFromExpiredToken(string token);
    Guid GetUserIdFromToken(string token);
}