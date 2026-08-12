namespace EBayClone.Application.DTOs.BusinessProfile;

public record BusinessProfileResponse(
    Guid Id,
    Guid UserId,
    string CompanyName,
    string GstNumber,
    string PanNumber,
    string? BusinessAddress,
    string? BusinessPhone,
    string? BusinessEmail,
    string? BusinessWebsite,
    string VerificationStatus,
    string? RejectionReason,
    DateTime? VerifiedAt,
    IReadOnlyList<DocumentResponse> Documents,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record DocumentResponse(
    Guid Id,
    string FileName,
    string FileUrl,
    string DocumentType,
    long FileSizeBytes,
    string ContentType,
    DateTime CreatedAt
);

public record AdminBusinessProfileResponse(
    Guid Id,
    Guid UserId,
    string UserEmail,
    string UserFullName,
    string CompanyName,
    string GstNumber,
    string PanNumber,
    string VerificationStatus,
    int DocumentCount,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
