using EBayClone.Application.Common;
using EBayClone.Application.DTOs.BusinessProfile;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class BusinessProfileService(
    IRepository<BusinessProfile> profileRepository,
    IRepository<UserDocument> documentRepository,
    IRepository<User> userRepository,
    IFileStorageService fileStorageService,
    IBackgroundEmailService backgroundEmailService,
    ILogger<BusinessProfileService> logger) : IBusinessProfileService
{
    public async Task<BusinessProfileResponse?> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.Query()
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(p => p.UserId == userId, ct);

        return profile is null ? null : MapToResponse(profile);
    }

    public async Task<BusinessProfileResponse> SubmitAsync(Guid userId, BusinessProfileRequest request, CancellationToken ct = default)
    {
        var existing = await profileRepository.FirstOrDefaultAsync(p => p.UserId == userId, ct);
        if (existing is not null)
            throw new InvalidOperationException("Business profile already exists. Use update to modify it.");

        var gstUpper = request.GstNumber.ToUpper();
        var gstTaken = await profileRepository.ExistsAsync(p => p.GstNumber == gstUpper, ct);
        if (gstTaken)
            throw new InvalidOperationException("A business with this GST number is already registered.");

        var panUpper = request.PanNumber.ToUpper();
        var panTaken = await profileRepository.ExistsAsync(p => p.PanNumber == panUpper, ct);
        if (panTaken)
            throw new InvalidOperationException("A business with this PAN number is already registered.");

        var profile = new BusinessProfile
        {
            UserId = userId,
            CompanyName = request.CompanyName.Trim(),
            GstNumber = request.GstNumber.ToUpper().Trim(),
            PanNumber = request.PanNumber.ToUpper().Trim(),
            BusinessAddress = request.BusinessAddress?.Trim(),
            BusinessPhone = request.BusinessPhone?.Trim(),
            BusinessEmail = request.BusinessEmail?.ToLower().Trim(),
            BusinessWebsite = request.BusinessWebsite?.Trim(),
            VerificationStatus = VerificationStatus.Draft,
        };

        await profileRepository.AddAsync(profile, ct);
        await profileRepository.SaveChangesAsync(ct);

        logger.LogInformation("Business profile saved (draft) for user {UserId}", userId);

        return MapToResponse(profile);
    }

    public async Task<BusinessProfileResponse> UpdateAsync(Guid userId, BusinessProfileRequest request, CancellationToken ct = default)
    {
        var profile = await profileRepository.Query()
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Business profile not found.");

        if (profile.VerificationStatus == VerificationStatus.Verified)
            throw new InvalidOperationException("Verified profiles cannot be edited. Contact support for changes.");

        if (profile.VerificationStatus is VerificationStatus.UnderReview or VerificationStatus.Pending)
            throw new InvalidOperationException("Profile is under review and cannot be edited.");

        var updGst = request.GstNumber.ToUpper();
        var gstTaken = await profileRepository.ExistsAsync(
            p => p.GstNumber == updGst && p.Id != profile.Id, ct);
        if (gstTaken)
            throw new InvalidOperationException("A business with this GST number is already registered.");

        var updPan = request.PanNumber.ToUpper();
        var panTaken = await profileRepository.ExistsAsync(
            p => p.PanNumber == updPan && p.Id != profile.Id, ct);
        if (panTaken)
            throw new InvalidOperationException("A business with this PAN number is already registered.");

        profile.CompanyName = request.CompanyName.Trim();
        profile.GstNumber = request.GstNumber.ToUpper().Trim();
        profile.PanNumber = request.PanNumber.ToUpper().Trim();
        profile.BusinessAddress = request.BusinessAddress?.Trim();
        profile.BusinessPhone = request.BusinessPhone?.Trim();
        profile.BusinessEmail = request.BusinessEmail?.ToLower().Trim();
        profile.BusinessWebsite = request.BusinessWebsite?.Trim();
        profile.VerificationStatus = VerificationStatus.Draft;
        profile.RejectionReason = null;
        profile.UpdatedAt = DateTime.UtcNow;

        profileRepository.Update(profile);
        await profileRepository.SaveChangesAsync(ct);

        logger.LogInformation("Business profile updated (draft) for user {UserId}", userId);

        return MapToResponse(profile);
    }

    public async Task<BusinessProfileResponse> SubmitForReviewAsync(Guid userId, CancellationToken ct = default)
    {
        var profile = await profileRepository.Query()
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Business profile not found. Please save your details first.");

        if (profile.VerificationStatus is VerificationStatus.Verified)
            throw new InvalidOperationException("Profile is already verified.");

        if (profile.VerificationStatus is VerificationStatus.Pending or VerificationStatus.UnderReview)
            throw new InvalidOperationException("Profile is already submitted for review.");

        var docCount = profile.Documents.Count(d => !d.IsDeleted);
        if (docCount == 0)
            throw new InvalidOperationException("Upload at least one supporting document before submitting for review.");

        profile.VerificationStatus = VerificationStatus.Pending;
        profile.UpdatedAt = DateTime.UtcNow;

        profileRepository.Update(profile);
        await profileRepository.SaveChangesAsync(ct);

        var user = await userRepository.GetByIdAsync(userId, ct);
        if (user is not null)
        {
            backgroundEmailService.EnqueueEmail(s =>
                s.SendBusinessProfileSubmittedAsync(user.Email, user.FirstName, CancellationToken.None));
        }

        logger.LogInformation("Business profile submitted for review: user {UserId}", userId);

        return MapToResponse(profile);
    }

    public async Task<DocumentResponse> UploadDocumentAsync(
        Guid userId, Stream fileStream, string fileName, string contentType,
        long fileSize, DocumentType documentType, CancellationToken ct = default)
    {
        var profile = await profileRepository.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Business profile not found. Please submit your profile first.");

        if (profile.VerificationStatus == VerificationStatus.Verified)
            throw new InvalidOperationException("Cannot upload documents to a verified profile.");

        var fileUrl = await fileStorageService.UploadAsync(fileStream, fileName, contentType, "documents", ct);

        var document = new UserDocument
        {
            BusinessProfileId = profile.Id,
            FileName = fileName,
            FileUrl = fileUrl,
            DocumentType = documentType,
            FileSizeBytes = fileSize,
            ContentType = contentType,
        };

        await documentRepository.AddAsync(document, ct);
        await documentRepository.SaveChangesAsync(ct);

        logger.LogInformation("Document {FileName} uploaded for user {UserId}", fileName, userId);

        return MapDocumentToResponse(document);
    }

    public async Task DeleteDocumentAsync(Guid userId, Guid documentId, CancellationToken ct = default)
    {
        var profile = await profileRepository.FirstOrDefaultAsync(p => p.UserId == userId, ct)
            ?? throw new KeyNotFoundException("Business profile not found.");

        var document = await documentRepository.GetByIdAsync(documentId, ct)
            ?? throw new KeyNotFoundException("Document not found.");

        if (document.BusinessProfileId != profile.Id)
            throw new UnauthorizedAccessException("Document does not belong to your profile.");

        await fileStorageService.DeleteAsync(document.FileUrl, ct);
        documentRepository.SoftDelete(document);
        await documentRepository.SaveChangesAsync(ct);

        logger.LogInformation("Document {DocumentId} deleted for user {UserId}", documentId, userId);
    }

    public async Task<PagedResult<AdminBusinessProfileResponse>> GetAllAsync(
        PagedQuery query, string? statusFilter, CancellationToken ct = default)
    {
        var q = profileRepository.Query()
            .Include(p => p.User)
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .AsNoTracking();

        if (!string.IsNullOrWhiteSpace(statusFilter) &&
            Enum.TryParse<VerificationStatus>(statusFilter, true, out var status))
            q = q.Where(p => p.VerificationStatus == status);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(p =>
                p.CompanyName.Contains(query.Search) ||
                p.GstNumber.Contains(query.Search) ||
                p.User.Email.Contains(query.Search));

        q = (query.SortBy?.ToLowerInvariant(), query.SortDirection?.ToLowerInvariant()) switch
        {
            ("company", "asc") => q.OrderBy(p => p.CompanyName),
            ("company", _) => q.OrderByDescending(p => p.CompanyName),
            ("owner", "asc") => q.OrderBy(p => p.User.FirstName).ThenBy(p => p.User.LastName),
            ("owner", _) => q.OrderByDescending(p => p.User.FirstName).ThenByDescending(p => p.User.LastName),
            ("gst", "asc") => q.OrderBy(p => p.GstNumber).ThenBy(p => p.PanNumber),
            ("gst", _) => q.OrderByDescending(p => p.GstNumber).ThenByDescending(p => p.PanNumber),
            ("documents", "asc") => q.OrderBy(p => p.Documents.Count(d => !d.IsDeleted)),
            ("documents", _) => q.OrderByDescending(p => p.Documents.Count(d => !d.IsDeleted)),
            ("status", "asc") => q.OrderBy(p => p.VerificationStatus),
            ("status", _) => q.OrderByDescending(p => p.VerificationStatus),
            ("updatedat", "asc") => q.OrderBy(p => p.UpdatedAt),
            ("updatedat", _) => q.OrderByDescending(p => p.UpdatedAt),
            ("createdat", "asc") => q.OrderBy(p => p.CreatedAt),
            _ => q.OrderByDescending(p => p.CreatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<AdminBusinessProfileResponse>.Create(
            items.Select(MapToAdminResponse).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<BusinessProfileResponse> ReviewAsync(
        Guid profileId, Guid adminId, bool isApproved, string? rejectionReason, CancellationToken ct = default)
    {
        var profile = await profileRepository.Query()
            .Include(p => p.Documents.Where(d => !d.IsDeleted))
            .Include(p => p.User)
            .FirstOrDefaultAsync(p => p.Id == profileId, ct)
            ?? throw new KeyNotFoundException("Business profile not found.");

        profile.VerificationStatus = isApproved ? VerificationStatus.Verified : VerificationStatus.Rejected;
        profile.RejectionReason = isApproved ? null : rejectionReason;
        profile.VerifiedAt = isApproved ? DateTime.UtcNow : null;
        profile.VerifiedByAdminId = isApproved ? adminId : null;
        profile.UpdatedAt = DateTime.UtcNow;

        profileRepository.Update(profile);
        await profileRepository.SaveChangesAsync(ct);

        var userEmail = profile.User.Email;
        var userName = profile.User.FirstName;

        // Send email in background using scoped service
        backgroundEmailService.EnqueueEmail(s => 
            s.SendBusinessProfileReviewedAsync(
                userEmail, userName, isApproved, rejectionReason, CancellationToken.None));

        logger.LogInformation("Business profile {ProfileId} {Action} by admin {AdminId}",
            profileId, isApproved ? "approved" : "rejected", adminId);

        return MapToResponse(profile);
    }

    private static BusinessProfileResponse MapToResponse(BusinessProfile p) => new(
        p.Id, p.UserId,
        p.CompanyName, p.GstNumber, p.PanNumber,
        p.BusinessAddress, p.BusinessPhone, p.BusinessEmail, p.BusinessWebsite,
        p.VerificationStatus.ToString(),
        p.RejectionReason,
        p.VerifiedAt,
        p.Documents.Where(d => !d.IsDeleted).Select(MapDocumentToResponse).ToList(),
        p.CreatedAt, p.UpdatedAt
    );

    private static AdminBusinessProfileResponse MapToAdminResponse(BusinessProfile p) => new(
        p.Id, p.UserId,
        p.User.Email,
        $"{p.User.FirstName} {p.User.LastName}",
        p.CompanyName, p.GstNumber, p.PanNumber,
        p.VerificationStatus.ToString(),
        p.Documents.Count(d => !d.IsDeleted),
        p.CreatedAt, p.UpdatedAt
    );

    private static DocumentResponse MapDocumentToResponse(UserDocument d) => new(
        d.Id, d.FileName, d.FileUrl,
        d.DocumentType.ToString(),
        d.FileSizeBytes, d.ContentType,
        d.CreatedAt
    );
}
