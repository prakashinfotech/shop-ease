namespace EBayClone.Application.DTOs.Auth;

public record CheckEmailRequest(string Email);

public record CheckEmailResponse(bool Exists);

public record ForgotPasswordRequest(string Email);

public record ResetPasswordRequest(string Token, string NewPassword, string ConfirmPassword);

public record VerifyEmailRequest(string Token);
