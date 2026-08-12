using System.Security.Claims;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Auth;
using EBayClone.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace EBayClone.API.Controllers;

[ApiController]
[Route("api/v1/[controller]")]
public class AuthController(IAuthService authService) : ControllerBase
{
    [HttpPost("register")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Register(
        [FromBody] RegisterRequest request, CancellationToken ct)
    {
        var result = await authService.RegisterAsync(request, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Registration successful. Please verify your email."));
    }

    [HttpPost("login")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Login(
        [FromBody] LoginRequest request, CancellationToken ct)
    {
        var result = await authService.LoginAsync(request, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result, "Login successful"));
    }

    [HttpPost("check-email")]
    public async Task<ActionResult<ApiResponse<CheckEmailResponse>>> CheckEmail(
        [FromBody] CheckEmailRequest request, CancellationToken ct)
    {
        var result = await authService.CheckEmailAsync(request, ct);
        return Ok(ApiResponse<CheckEmailResponse>.Ok(result));
    }

    [HttpPost("refresh")]
    public async Task<ActionResult<ApiResponse<AuthResponse>>> Refresh(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken, ct);
        return Ok(ApiResponse<AuthResponse>.Ok(result));
    }

    [HttpPost("logout")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> Logout(
        [FromBody] RefreshRequest request, CancellationToken ct)
    {
        await authService.RevokeTokenAsync(request.RefreshToken, ct);
        return Ok(ApiResponse.Ok("Logged out successfully"));
    }

    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<ApiResponse<UserDto>>> Me(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue("sub")!);
        var user = await authService.GetCurrentUserAsync(userId, ct);
        return Ok(ApiResponse<UserDto>.Ok(user));
    }

    [HttpPost("verify-email")]
    public async Task<ActionResult<ApiResponse<UserDto>>> VerifyEmail(
        [FromBody] VerifyEmailRequest request, CancellationToken ct)
    {
        var user = await authService.VerifyEmailAsync(request.Token, ct);
        return Ok(ApiResponse<UserDto>.Ok(user, "Email verified successfully"));
    }

    [HttpPost("resend-verification")]
    [Authorize]
    public async Task<ActionResult<ApiResponse>> ResendVerification(CancellationToken ct)
    {
        var userId = Guid.Parse(User.FindFirstValue("sub")!);
        await authService.ResendVerificationEmailAsync(userId, ct);
        return Ok(ApiResponse.Ok("Verification email sent. Please check your inbox."));
    }

    [HttpPost("forgot-password")]
    public async Task<ActionResult<ApiResponse>> ForgotPassword(
        [FromBody] ForgotPasswordRequest request, CancellationToken ct)
    {
        await authService.ForgotPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok("If an account with that email exists, a reset link has been sent."));
    }

    [HttpPost("reset-password")]
    public async Task<ActionResult<ApiResponse>> ResetPassword(
        [FromBody] ResetPasswordRequest request, CancellationToken ct)
    {
        await authService.ResetPasswordAsync(request, ct);
        return Ok(ApiResponse.Ok("Password reset successfully. You can now log in with your new password."));
    }
}
