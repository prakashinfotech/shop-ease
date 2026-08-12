using EBayClone.Application.Common;
using EBayClone.Application.DTOs.EmailTemplates;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;

namespace EBayClone.Application.Interfaces;

public interface IEmailTemplateService
{
    Task<EmailTemplateResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<PagedResult<EmailTemplateResponse>> GetAllAsync(int page, int pageSize, EmailTemplateType? type, CancellationToken ct = default);
    Task<EmailTemplateResponse> CreateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default);
    Task<EmailTemplateResponse> UpdateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken ct = default);
    Task<EmailTemplateResponse> ActivateAsync(Guid id, CancellationToken ct = default);
    Task<EmailTemplateResponse> DeactivateAsync(Guid id, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<EmailTemplate?> GetActiveTemplateAsync(EmailTemplateType type, CancellationToken ct = default);
}
