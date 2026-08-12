using EBayClone.Application.Common;
using EBayClone.Application.DTOs.BusinessProfile;
using EBayClone.Domain.Enums;

namespace EBayClone.Application.Interfaces;

public interface IBusinessProfileService
{
    Task<BusinessProfileResponse?> GetByUserIdAsync(Guid userId, CancellationToken ct = default);
    Task<BusinessProfileResponse> SubmitAsync(Guid userId, BusinessProfileRequest request, CancellationToken ct = default);
    Task<BusinessProfileResponse> UpdateAsync(Guid userId, BusinessProfileRequest request, CancellationToken ct = default);
    Task<BusinessProfileResponse> SubmitForReviewAsync(Guid userId, CancellationToken ct = default);
    Task<DocumentResponse> UploadDocumentAsync(Guid userId, Stream fileStream, string fileName, string contentType, long fileSize, DocumentType documentType, CancellationToken ct = default);
    Task DeleteDocumentAsync(Guid userId, Guid documentId, CancellationToken ct = default);

    // Admin
    Task<PagedResult<AdminBusinessProfileResponse>> GetAllAsync(PagedQuery query, string? statusFilter, CancellationToken ct = default);
    Task<BusinessProfileResponse> ReviewAsync(Guid profileId, Guid adminId, bool isApproved, string? rejectionReason, CancellationToken ct = default);
}
