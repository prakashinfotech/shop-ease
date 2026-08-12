using EBayClone.Domain.Enums;

namespace EBayClone.Application.DTOs.BusinessProfile;

public record BusinessProfileRequest(
    string CompanyName,
    string GstNumber,
    string PanNumber,
    string? BusinessAddress = null,
    string? BusinessPhone = null,
    string? BusinessEmail = null,
    string? BusinessWebsite = null
);

public record ReviewBusinessProfileRequest(
    bool IsApproved,
    string? RejectionReason = null
);

public record UploadDocumentRequest(
    DocumentType DocumentType
);
