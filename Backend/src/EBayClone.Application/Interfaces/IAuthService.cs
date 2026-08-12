using EBayClone.Application.DTOs.Auth;

namespace EBayClone.Application.Interfaces;

public interface IAuthService
{
    Task<AuthResponse> RegisterAsync(RegisterRequest request, CancellationToken ct = default);
    Task<CheckEmailResponse> CheckEmailAsync(CheckEmailRequest request, CancellationToken ct = default);
    Task<AuthResponse> LoginAsync(LoginRequest request, CancellationToken ct = default);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken, CancellationToken ct = default);
    Task RevokeTokenAsync(string refreshToken, CancellationToken ct = default);
    Task<UserDto> GetCurrentUserAsync(Guid userId, CancellationToken ct = default);

    // Email verification
    Task<UserDto> VerifyEmailAsync(string token, CancellationToken ct = default);
    Task ResendVerificationEmailAsync(Guid userId, CancellationToken ct = default);

    // Password reset
    Task ForgotPasswordAsync(ForgotPasswordRequest request, CancellationToken ct = default);
    Task ResetPasswordAsync(ResetPasswordRequest request, CancellationToken ct = default);
}
