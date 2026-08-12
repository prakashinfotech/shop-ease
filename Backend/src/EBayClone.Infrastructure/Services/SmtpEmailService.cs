using EBayClone.Application.Interfaces;
using EBayClone.Domain.Enums;
using MailKit.Net.Smtp;
using MailKit.Security;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using MimeKit;

namespace EBayClone.Infrastructure.Services;

public class SmtpEmailService(
    IEmailTemplateService templateService,
    IConfiguration configuration,
    ILogger<SmtpEmailService> logger) : IEmailService
{
    private readonly string _frontendUrl =
        configuration["AppSettings:FrontendUrl"]
        ?? throw new InvalidOperationException("AppSettings:FrontendUrl is not configured.");

    private readonly string _supportEmail =
        configuration["SmtpSettings:FromEmail"] ?? "support@shopease.com";

    // ── Public IEmailService methods ────────────────────────────────────

    public Task SendEmailVerificationAsync(string email, string firstName, string token, CancellationToken ct = default)
    {
        var link = $"{_frontendUrl}/verify-email?token={Uri.EscapeDataString(token)}";
        return SendTemplatedAsync(EmailTemplateType.EmailVerification, email, new()
        {
            ["UserName"] = firstName,
            ["VerificationLink"] = link,
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);
    }

    public Task SendPasswordResetAsync(string email, string firstName, string token, CancellationToken ct = default)
    {
        var link = $"{_frontendUrl}/reset-password?token={Uri.EscapeDataString(token)}";
        return SendTemplatedAsync(EmailTemplateType.ForgotPassword, email, new()
        {
            ["UserName"] = firstName,
            ["ResetLink"] = link,
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);
    }

    public Task SendPasswordChangedNotificationAsync(string email, string firstName, CancellationToken ct = default)
        => SendTemplatedAsync(EmailTemplateType.PasswordChanged, email, new()
        {
            ["UserName"] = firstName,
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);

    public Task SendBusinessProfileSubmittedAsync(string email, string firstName, CancellationToken ct = default)
    {
        logger.LogInformation("[EMAIL] Business Profile Submitted | To: {Email} | Name: {Name}", email, firstName);
        return Task.CompletedTask;
    }

    public Task SendBusinessProfileReviewedAsync(string email, string firstName, bool isApproved, string? rejectionReason, CancellationToken ct = default)
    {
        logger.LogInformation(
            "[EMAIL] Business Profile Reviewed | To: {Email} | Name: {Name} | Approved: {Approved}",
            email, firstName, isApproved);
        return Task.CompletedTask;
    }

    public Task SendListingApprovedAsync(string email, string firstName, string listingTitle, string listingUrl, CancellationToken ct = default)
        => SendTemplatedAsync(EmailTemplateType.ListingApproved, email, new()
        {
            ["UserName"] = firstName,
            ["ListingTitle"] = listingTitle,
            ["ListingUrl"] = listingUrl,
            ["ApprovedAt"] = DateTime.UtcNow.ToString("MMMM dd, yyyy 'at' HH:mm UTC"),
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);

    public Task SendListingRejectedAsync(string email, string firstName, string listingTitle, string rejectionReason, CancellationToken ct = default)
        => SendTemplatedAsync(EmailTemplateType.ListingRejected, email, new()
        {
            ["UserName"] = firstName,
            ["ListingTitle"] = listingTitle,
            ["RejectionReason"] = rejectionReason,
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);

    public Task SendListingPendingApprovalAsync(string email, string firstName, string listingTitle, CancellationToken ct = default)
        => SendTemplatedAsync(EmailTemplateType.ListingPendingApproval, email, new()
        {
            ["UserName"] = firstName,
            ["ListingTitle"] = listingTitle,
            ["Year"] = DateTime.UtcNow.Year.ToString(),
            ["SupportEmail"] = _supportEmail,
        }, ct);

    // ── Internal helpers ────────────────────────────────────────────────

    private async Task SendTemplatedAsync(
        EmailTemplateType type,
        string toEmail,
        Dictionary<string, string> context,
        CancellationToken ct)
    {
        try
        {
            var template = await templateService.GetActiveTemplateAsync(type, ct);
            if (template is null)
            {
                logger.LogWarning("[EMAIL] No active template for {Type}. Skipping send to {Email}", type, toEmail);
                return;
            }

            var subject = ResolvePlaceholders(template.Subject, context);
            var body = ResolvePlaceholders(template.HtmlBody, context);

            await SendAsync(toEmail, subject, body, ct);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "[EMAIL] Send canceled for template {Type} to {Email}", type, toEmail);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[EMAIL] Failed to render/send template {Type} to {Email}", type, toEmail);
        }
    }

    private async Task SendAsync(string toEmail, string subject, string htmlBody, CancellationToken ct)
    {
        var host = configuration["SmtpSettings:Host"];
        if (string.IsNullOrWhiteSpace(host))
        {
            logger.LogWarning("[EMAIL] SMTP Host is not configured. Email to {Email} will only be logged to console.", toEmail);
            logger.LogInformation("[EMAIL-CONSOLE] To: {Email} | Subject: {Subject}", toEmail, subject);
            return;
        }

        // Use a dedicated timeout for email sending (30 seconds)
        // We don't link it to 'ct' here because we want the email to finish sending 
        // even if the HTTP request is cancelled (e.g. user closes the browser),
        // as the action (like password reset) has already been committed to the database.
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
        var timeoutToken = cts.Token;

        try
        {
            var port = int.TryParse(configuration["SmtpSettings:Port"], out var p) ? p : 587;
            var username = configuration["SmtpSettings:Username"] ?? string.Empty;
            var password = configuration["SmtpSettings:Password"] ?? string.Empty;
            var fromEmail = configuration["SmtpSettings:FromEmail"] ?? "noreply@shopease.com";
            var fromName = string.IsNullOrWhiteSpace(configuration["SmtpSettings:FromName"])
                ? "ShopEase"
                : configuration["SmtpSettings:FromName"]!;
            var enableSsl = !string.Equals(configuration["SmtpSettings:EnableSsl"], "false", StringComparison.OrdinalIgnoreCase);

            var message = new MimeMessage();
            message.From.Add(new MailboxAddress(fromName, fromEmail));
            message.To.Add(MailboxAddress.Parse(toEmail));
            message.Subject = subject;
            message.Body = new BodyBuilder { HtmlBody = htmlBody }.ToMessageBody();

            using var client = new SmtpClient();
            
            // Set a client-side timeout as well
            client.Timeout = 30000; 

            logger.LogDebug("[EMAIL] Connecting to SMTP host {Host}:{Port} (SSL: {EnableSsl})...", host, port, enableSsl);
            await client.ConnectAsync(host, port,
                enableSsl ? SecureSocketOptions.StartTls : SecureSocketOptions.None, timeoutToken);

            if (!string.IsNullOrEmpty(username))
            {
                logger.LogDebug("[EMAIL] Authenticating as {Username}...", username);
                await client.AuthenticateAsync(username, password, timeoutToken);
            }

            logger.LogDebug("[EMAIL] Sending message to {Email}...", toEmail);
            await client.SendAsync(message, timeoutToken);
            
            await client.DisconnectAsync(quit: true, timeoutToken);

            logger.LogInformation("[EMAIL] Successfully sent '{Subject}' to {Email}", subject, toEmail);
        }
        catch (OperationCanceledException ex) when (timeoutToken.IsCancellationRequested)
        {
            logger.LogError(ex, "[EMAIL] SMTP operation timed out (30s) while sending to {Email}: {Subject}", toEmail, subject);
        }
        catch (OperationCanceledException ex)
        {
            logger.LogWarning(ex, "[EMAIL] SMTP operation was cancelled by external token for {Email}: {Subject}", toEmail, subject);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "[EMAIL] Failed to send email to {Email}: {Subject}. Error: {Message}", toEmail, subject, ex.Message);
        }
    }

    private static string ResolvePlaceholders(string template, Dictionary<string, string> context)
        => context.Aggregate(template, (current, kv) =>
            current.Replace($"{{{{{kv.Key}}}}}", kv.Value, StringComparison.Ordinal));
}
