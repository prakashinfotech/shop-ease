using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Infrastructure.Data.Seed;

public static class EmailTemplateSeeder
{
    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var templates = BuildTemplates();
        foreach (var seed in templates)
        {
            var existing = await db.EmailTemplates
                .IgnoreQueryFilters()
                .FirstOrDefaultAsync(t => t.TemplateType == seed.Type && t.Name == seed.Name, ct);

            if (existing is not null)
            {
                existing.Subject = seed.Subject;
                existing.HtmlBody = seed.HtmlBody;
                existing.UpdatedAt = DateTime.UtcNow;
                continue;
            }

            db.EmailTemplates.Add(new EmailTemplate
            {
                Name = seed.Name,
                Subject = seed.Subject,
                HtmlBody = seed.HtmlBody,
                TemplateType = seed.Type,
                IsActive = true,
                Version = 1,
            });
        }

        await db.SaveChangesAsync(ct);
    }

    private static IEnumerable<(string Name, string Subject, string HtmlBody, EmailTemplateType Type)> BuildTemplates()
    {
        yield return (
            "Email Verification",
            "Verify your ShopEase email address",
            WrapInLayout("Verify Your Email", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 24px">Thanks for signing up! Please verify your email address to activate your account.</p>
                <div style="text-align:center;margin:32px 0">
                  <a href="{{VerificationLink}}" style="background:#0064d2;color:#fff;text-decoration:none;padding:14px 36px;border-radius:6px;font-size:15px;font-weight:700;display:inline-block">Verify Email Address</a>
                </div>
                <p style="font-size:13px;color:#888;margin:24px 0 0">This link expires in 24 hours. If you didn't create an account, you can safely ignore this email.</p>
                <p style="font-size:13px;color:#aaa;margin:12px 0 0;word-break:break-all">Or copy this link: {{VerificationLink}}</p>
                """),
            EmailTemplateType.EmailVerification
        );

        yield return (
            "Forgot Password",
            "Reset your ShopEase password",
            WrapInLayout("&#128065; Reset Your Password", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 24px">We received a request to reset your password. Click the button below to choose a new one.</p>
                <div style="text-align:center;margin:32px 0">
                  <a href="{{ResetLink}}" style="background:#e53238;color:#fff;text-decoration:none;padding:14px 36px;border-radius:6px;font-size:15px;font-weight:700;display:inline-block">Reset Password</a>
                </div>
                <p style="font-size:13px;color:#888;margin:24px 0 0">This link expires in 1 hour. If you didn't request a password reset, please ignore this email — your password will not change.</p>
                <p style="font-size:13px;color:#aaa;margin:12px 0 0;word-break:break-all">Or copy this link: {{ResetLink}}</p>
                """),
            EmailTemplateType.ForgotPassword
        );

        yield return (
            "Password Changed",
            "Your ShopEase password has been changed",
            WrapInLayout("Password Changed", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 24px">Your account password was successfully changed.</p>
                <div style="background:#fff8e1;border-left:4px solid #f5af02;padding:14px 18px;border-radius:4px;margin:0 0 24px">
                  <p style="font-size:14px;color:#555;margin:0">If you didn't make this change, please contact us immediately at <a href="mailto:{{SupportEmail}}" style="color:#0064d2">{{SupportEmail}}</a>.</p>
                </div>
                """),
            EmailTemplateType.PasswordChanged
        );

        yield return (
            "Listing Pending Approval",
            "Your listing has been submitted for review",
            WrapInLayout("Listing Submitted for Review", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 12px">Your listing has been submitted and is now under review:</p>
                <div style="background:#f5f5f5;border:1px solid #ddd;border-radius:6px;padding:16px 20px;margin:0 0 24px">
                  <p style="font-size:16px;font-weight:700;color:#111;margin:0">{{ListingTitle}}</p>
                </div>
                <p style="font-size:15px;color:#444;margin:0 0 24px">Our team typically reviews listings within 1–2 business days. You'll receive an email once a decision has been made.</p>
                <div style="background:#e8f4fd;border-left:4px solid #0064d2;padding:14px 18px;border-radius:4px">
                  <p style="font-size:14px;color:#555;margin:0">While your listing is under review it will not be visible to buyers.</p>
                </div>
                """),
            EmailTemplateType.ListingPendingApproval
        );

        yield return (
            "Listing Approved",
            "Your listing has been approved!",
            WrapInLayout("Listing Approved", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 12px">Great news! Your listing has been approved by our admin team and is now live on the marketplace:</p>
                <div style="background:#f5f5f5;border:1px solid #ddd;border-radius:6px;padding:16px 20px;margin:0 0 16px">
                  <p style="font-size:16px;font-weight:700;color:#111;margin:0 0 8px">{{ListingTitle}}</p>
                  <p style="font-size:13px;color:#888;margin:0">Approved on {{ApprovedAt}}</p>
                </div>
                <div style="text-align:center;margin:0 0 28px">
                  <a href="{{ListingUrl}}" style="background:#86b817;color:#fff;text-decoration:none;padding:14px 36px;border-radius:6px;font-size:15px;font-weight:700;display:inline-block">View Your Listing</a>
                </div>
                <p style="font-size:14px;color:#888">Buyers can now find and purchase your item. Good luck with your sale!</p>
                """),
            EmailTemplateType.ListingApproved
        );

        yield return (
            "Listing Rejected",
            "Update needed on your listing",
            WrapInLayout("Listing Requires Changes", """
                <p style="font-size:15px;color:#444;margin:0 0 20px">Hi <strong>{{UserName}}</strong>,</p>
                <p style="font-size:15px;color:#444;margin:0 0 12px">Unfortunately your listing could not be approved at this time:</p>
                <div style="background:#f5f5f5;border:1px solid #ddd;border-radius:6px;padding:16px 20px;margin:0 0 20px">
                  <p style="font-size:16px;font-weight:700;color:#111;margin:0">{{ListingTitle}}</p>
                </div>
                <div style="background:#fff0f0;border-left:4px solid #e53238;padding:14px 18px;border-radius:4px;margin:0 0 24px">
                  <p style="font-size:14px;font-weight:600;color:#c00;margin:0 0 6px">Reason:</p>
                  <p style="font-size:14px;color:#555;margin:0">{{RejectionReason}}</p>
                </div>
                <p style="font-size:14px;color:#444;margin:0 0 12px">Please update your listing to address the feedback above and resubmit for approval.</p>
                <p style="font-size:13px;color:#888">If you have questions, contact us at <a href="mailto:{{SupportEmail}}" style="color:#0064d2">{{SupportEmail}}</a>.</p>
                """),
            EmailTemplateType.ListingRejected
        );
    }

    private static string WrapInLayout(string heading, string body) => $$$"""
        <!DOCTYPE html>
        <html lang="en">
        <head><meta charset="UTF-8"><meta name="viewport" content="width=device-width,initial-scale=1"></head>
        <body style="margin:0;padding:0;background:#f3f3f3;font-family:Arial,sans-serif">
          <table width="100%" cellpadding="0" cellspacing="0" style="background:#f3f3f3;padding:32px 16px">
            <tr><td align="center">
              <table width="600" cellpadding="0" cellspacing="0" style="max-width:600px;width:100%;background:#fff;border-radius:8px;overflow:hidden;box-shadow:0 2px 8px rgba(0,0,0,.08)">
                <!-- Header -->
                <tr>
                  <td style="background:#0064d2;padding:24px 32px">
                    <table cellpadding="0" cellspacing="0">
                      <tr>
                        <td style="font-size:28px;font-weight:800;color:#fff;letter-spacing:-1px">
                          <span style="color:#fff">e</span><span style="color:#f5af02">B</span><span style="color:#fff">a</span><span style="color:#86b817">y</span>
                          <span style="font-size:14px;font-weight:400;color:rgba(255,255,255,.75);margin-left:6px">Clone</span>
                        </td>
                      </tr>
                    </table>
                  </td>
                </tr>
                <!-- Body -->
                <tr>
                  <td style="padding:36px 32px 20px">
                    <h1 style="font-size:22px;font-weight:700;color:#111820;margin:0 0 24px">{{{heading}}}</h1>
                    {{{body}}}
                  </td>
                </tr>
                <!-- Footer -->
                <tr>
                  <td style="background:#f8f8f8;border-top:1px solid #eee;padding:20px 32px;text-align:center">
                    <p style="font-size:12px;color:#aaa;margin:0">&copy; {{Year}} ShopEase. All rights reserved.</p>
                    <p style="font-size:12px;color:#aaa;margin:4px 0 0">Questions? <a href="mailto:{{SupportEmail}}" style="color:#0064d2;text-decoration:none">{{SupportEmail}}</a></p>
                  </td>
                </tr>
              </table>
            </td></tr>
          </table>
        </body>
        </html>
        """;
}
