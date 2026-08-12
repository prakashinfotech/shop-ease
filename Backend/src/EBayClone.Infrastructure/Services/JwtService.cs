using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;

namespace EBayClone.Infrastructure.Services;

public class JwtService(IConfiguration configuration) : IJwtService
{
    private readonly string _secret = configuration["JwtSettings:Secret"]
        ?? throw new InvalidOperationException("JwtSettings:Secret is not configured.");
    private readonly string _issuer = configuration["JwtSettings:Issuer"] ?? "ShopEaseApi";
    private readonly string _audience = configuration["JwtSettings:Audience"] ?? "ShopEaseFrontend";
    private readonly int _expiryMinutes = int.Parse(configuration["JwtSettings:ExpiryMinutes"] ?? "15");

    public string GenerateAccessToken(User user)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret));
        var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
            new Claim(ClaimTypes.Role, user.Role.ToString()),
            new Claim("firstName", user.FirstName),
            new Claim("accountType", user.AccountType.ToString()),
        };

        var token = new JwtSecurityToken(
            issuer: _issuer,
            audience: _audience,
            claims: claims,
            expires: DateTime.UtcNow.AddMinutes(_expiryMinutes),
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    public string GenerateRefreshToken()
    {
        var randomBytes = new byte[64];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(randomBytes);
        return Convert.ToBase64String(randomBytes);
    }

    public ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
    {
        var tokenValidationParameters = new TokenValidationParameters
        {
            ValidateAudience = false,
            ValidateIssuer = false,
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_secret)),
            ValidateLifetime = false,
        };

        var tokenHandler = new JwtSecurityTokenHandler();
        try
        {
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtToken ||
                !jwtToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
                return null;
            return principal;
        }
        catch
        {
            return null;
        }
    }

    public Guid GetUserIdFromToken(string token)
    {
        var principal = GetPrincipalFromExpiredToken(token);
        var sub = principal?.FindFirstValue(JwtRegisteredClaimNames.Sub);
        return Guid.TryParse(sub, out var id) ? id : Guid.Empty;
    }
}