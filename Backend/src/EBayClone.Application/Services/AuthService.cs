using System.Security.Cryptography;
using EBayClone.Application.DTOs.Auth;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class AuthService(
    IRepository<User> userRepository,
    IRepository<RefreshToken> refreshTokenRepository,
    IJwtService jwtService,
    IPasswordHasher passwordHasher,
    IBackgroundEmailService backgroundEmailService,
    ILogger<AuthService> logger) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default)
    {
        var emailLower = request.Email.ToLower();
        var exists = await userRepository.ExistsAsync(u => u.Email == emailLower, ct);
        if (exists)
            throw new InvalidOperationException("An account with this email already exists.");

        var verificationToken = GenerateSecureToken();

        var user = new User
        {
            FirstName = request.FirstName.Trim(),
            LastName = request.LastName.Trim(),
            Email = request.Email.ToLower().Trim(),
            PasswordHash = passwordHasher.Hash(request.Password),
            AccountType = request.AccountType,
            Role = UserRole.User,
            EmailVerificationToken = verificationToken,
            EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24),
        };

        await userRepository.AddAsync(user, ct);
        await userRepository.SaveChangesAsync(ct);

        logger.LogInformation("New user registered: {Email}", user.Email);

        // Send email in the background using the new scoped background service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendEmailVerificationAsync(user.Email, user.FirstName, verificationToken, CancellationToken.None));

        return await IssueTokensAsync(user, ct);
    }

    public async Task<CheckEmailResponse> CheckEmailAsync(CheckEmailRequest request, CancellationToken ct = default)
    {
        var email = request.Email.ToLower().Trim();
        var exists = await userRepository.ExistsAsync(u => u.Email == email, ct);
        return new CheckEmailResponse(exists);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default)
    {
        var loginEmail = request.Email.ToLower();
        var user = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Email == loginEmail, ct);

        if (user is null || !passwordHasher.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid email or password.");

        if (user.IsSuspended)
            throw new UnauthorizedAccessException("Your account has been suspended. Please contact support.");

        logger.LogInformation("User logged in: {Email}", user.Email);

        return await IssueTokensAsync(user, ct);
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken, ct);

        if (token is null || !token.IsActive)
            throw new UnauthorizedAccessException("Invalid or expired refresh token.");

        var user = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == token.UserId, ct)
            ?? throw new UnauthorizedAccessException("User not found.");

        token.IsRevoked = true;
        token.UpdatedAt = DateTime.UtcNow;
        refreshTokenRepository.Update(token);

        return await IssueTokensAsync(user, ct);
    }

    public async Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default)
    {
        var token = await refreshTokenRepository.FirstOrDefaultAsync(
            rt => rt.Token == refreshToken, ct);

        if (token is null) return;

        token.IsRevoked = true;
        token.UpdatedAt = DateTime.UtcNow;
        refreshTokenRepository.Update(token);
        await refreshTokenRepository.SaveChangesAsync(ct);
    }

    public async Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        return MapToDto(user);
    }

    public async Task<UserDto> VerifyEmailAsync(string token, CancellationToken ct = default)
    {
        var user = await userRepository.FirstOrDefaultAsync(
            u => u.EmailVerificationToken == token, ct);

        if (user is null || user.EmailVerificationTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired verification token.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        user.EmailVerificationTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        logger.LogInformation("Email verified for user: {Email}", user.Email);

        return MapToDto(user);
    }

    public async Task ResendVerificationEmailAsync(Guid userId, CancellationToken ct = default)
    {
        var user = await userRepository.GetByIdAsync(userId, ct)
            ?? throw new KeyNotFoundException("User not found.");

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Email is already verified.");

        var token = GenerateSecureToken();
        user.EmailVerificationToken = token;
        user.EmailVerificationTokenExpiry = DateTime.UtcNow.AddHours(24);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        // Send email in the background using the new scoped background service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendEmailVerificationAsync(user.Email, user.FirstName, token, CancellationToken.None));

        logger.LogInformation("Verification email resent to: {Email}", user.Email);
    }

    public async Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default)
    {
        // Always return success to prevent email enumeration attacks
        var forgotEmail = request.Email.ToLower();
        var user = await userRepository.FirstOrDefaultAsync(
            u => u.Email == forgotEmail, ct);

        if (user is null) return;

        var token = GenerateSecureToken();
        user.PasswordResetToken = token;
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        user.UpdatedAt = DateTime.UtcNow;
        userRepository.Update(user);
        await userRepository.SaveChangesAsync(ct);

        // Send email in the background using the new scoped background service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendPasswordResetAsync(user.Email, user.FirstName, token, CancellationToken.None));

        logger.LogInformation("Password reset requested for: {Email}", user.Email);
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default)
    {
        if (request.NewPassword != request.ConfirmPassword)
            throw new InvalidOperationException("Passwords do not match.");

        var user = await userRepository.FirstOrDefaultAsync(
            u => u.PasswordResetToken == request.Token, ct);

        if (user is null || user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Invalid or expired password reset token.");

        user.PasswordHash = passwordHasher.Hash(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        user.UpdatedAt = DateTime.UtcNow;
        await userRepository.SaveChangesAsync(ct);

        // Send email in the background using the new scoped background service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendPasswordChangedNotificationAsync(user.Email, user.FirstName, CancellationToken.None));

        logger.LogInformation("Password reset completed for: {Email}", user.Email);
    }

    private async Task<AuthResponse> IssueTokensAsync(User user, CancellationToken ct)
    {
        var accessToken = jwtService.GenerateAccessToken(user);
        var rawRefreshToken = jwtService.GenerateRefreshToken();

        var refreshToken = new RefreshToken
        {
            Token = rawRefreshToken,
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(7),
        };

        await refreshTokenRepository.AddAsync(refreshToken, ct);
        await refreshTokenRepository.SaveChangesAsync(ct);

        return new AuthResponse(MapToDto(user), accessToken, rawRefreshToken);
    }

    private static UserDto MapToDto(User user) => new(
        user.Id,
        user.FirstName,
        user.LastName,
        user.Email,
        user.AccountType.ToString(),
        user.Role.ToString(),
        user.IsEmailVerified,
        user.IsSuspended,
        user.BusinessProfile?.VerificationStatus.ToString()
    );

    private static string GenerateSecureToken()
    {
        var bytes = new byte[32];
        using var rng = RandomNumberGenerator.Create();
        rng.GetBytes(bytes);
        return Convert.ToBase64String(bytes).Replace("+", "-").Replace("/", "_").TrimEnd('=');
    }
}
