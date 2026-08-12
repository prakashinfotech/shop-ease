using EBayClone.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace EBayClone.Infrastructure.Services;

/// <summary>
/// Development-only email service. Logs email content to console/Serilog.
/// Swap this for an SMTP or SendGrid implementation in production.
/// </summary>
public class ConsoleEmailService(
    IConfiguration configuration,
    ILogger<ConsoleEmailService> logger) : IEmailService
{
    private readonly string _frontendUrl =
        configuration["AppSettings:FrontendUrl"]
        ?? throw new InvalidOperationException("AppSettings:FrontendUrl is not configured.");

    public Task SendEmailVerificationAsync(string email, string firstName, string token, CancellationToken ct = default)
    {
        var link = $"{_frontendUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        logger.LogInformation(
            "[EMAIL] Verify Email | To: {Email} | Name: {Name} | Link: {Link}",
            email, firstName, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordResetAsync(string email, string firstName, string token, CancellationToken ct = default)
    {
        var link = $"{_frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        logger.LogInformation(
            "[EMAIL] Password Reset | To: {Email} | Name: {Name} | Link: {Link}",
            email, firstName, link);
        return Task.CompletedTask;
    }

    public Task SendPasswordChangedNotificationAsync(string email, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] Password Changed | To: {Email} | Name: {Name}",
            email, firstName);
        return Task.CompletedTask;
    }

    public Task SendBusinessProfileSubmittedAsync(string email, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] Business Profile Submitted | To: {Email} | Name: {Name}",
            email, firstName);
        return Task.CompletedTask;
    }

    public Task SendBusinessProfileReviewedAsync(string email, string firstName, bool isApproved, string? rejectionReason, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] Business Profile Reviewed | To: {Email} | Name: {Name} | Approved: {Approved} | Reason: {Reason}",
            email, firstName, isApproved, rejectionReason ?? "N/A");
        return Task.CompletedTask;
    }

    public Task SendListingApprovedAsync(string email, string firstName, string listingTitle, string listingUrl, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Listing Approved | To: {Email} | Name: {Name} | Title: {Title}", email, firstName, listingTitle);
        return Task.CompletedTask;
    }

    public Task SendListingRejectedAsync(string email, string firstName, string listingTitle, string rejectionReason, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Listing Rejected | To: {Email} | Name: {Name} | Title: {Title} | Reason: {Reason}", email, firstName, listingTitle, rejectionReason);
        return Task.CompletedTask;
    }

    public Task SendListingPendingApprovalAsync(string email, string firstName, string listingTitle, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Listing Pending Approval | To: {Email} | Name: {Name} | Title: {Title}", email, firstName, listingTitle);
        return Task.CompletedTask;
    }
}
