namespace EBayClone.Domain.Enums;

public enum EmailTemplateType
{
    EmailVerification = 0,
    ForgotPassword = 1,
    PasswordChanged = 2,
    ListingPendingApproval = 3,
    ListingApproved = 4,
    ListingRejected = 5,
}
