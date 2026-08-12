using EBayClone.Application.Common;
using EBayClone.Application.DTOs.EmailTemplates;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class EmailTemplateService(
    IRepository<EmailTemplate> templateRepository,
    ILogger<EmailTemplateService> logger) : IEmailTemplateService
{
    public async Task<EmailTemplateResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Email template {id} not found.");
        return MapToResponse(template);
    }

    public async Task<PagedResult<EmailTemplateResponse>> GetAllAsync(
        int page, int pageSize, EmailTemplateType? type, CancellationToken ct = default)
    {
        page = page < 1 ? 1 : page;
        pageSize = Math.Clamp(pageSize, 1, 100);

        var q = templateRepository.Query().IgnoreQueryFilters().AsNoTracking();
        if (type.HasValue)
            q = q.Where(t => t.TemplateType == type.Value);

        q = q.OrderByDescending(t => t.CreatedAt);

        var total = await q.CountAsync(ct);
        var items = await q.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync(ct);

        return PagedResult<EmailTemplateResponse>.Create(
            items.Select(MapToResponse).ToList(), total, page, pageSize);
    }

    public async Task<EmailTemplateResponse> CreateAsync(CreateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var template = new EmailTemplate
        {
            Name = request.Name.Trim(),
            Subject = request.Subject.Trim(),
            HtmlBody = request.HtmlBody,
            TemplateType = request.TemplateType,
            IsActive = false,
            Version = 1,
        };

        await templateRepository.AddAsync(template, ct);
        await templateRepository.SaveChangesAsync(ct);

        logger.LogInformation("Email template created: {Id} ({Type})", template.Id, template.TemplateType);
        return MapToResponse(template);
    }

    public async Task<EmailTemplateResponse> UpdateAsync(Guid id, UpdateEmailTemplateRequest request, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Email template {id} not found.");

        template.Name = request.Name.Trim();
        template.Subject = request.Subject.Trim();
        template.HtmlBody = request.HtmlBody;
        template.Version++;
        template.UpdatedAt = DateTime.UtcNow;

        templateRepository.Update(template);
        await templateRepository.SaveChangesAsync(ct);

        return MapToResponse(template);
    }

    public async Task<EmailTemplateResponse> ActivateAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Email template {id} not found.");

        // Deactivate any other active template of the same type.
        var others = await templateRepository.Query()
            .Where(t => t.TemplateType == template.TemplateType && t.IsActive && t.Id != id)
            .ToListAsync(ct);

        foreach (var other in others)
        {
            other.IsActive = false;
            other.UpdatedAt = DateTime.UtcNow;
            templateRepository.Update(other);
        }

        template.IsActive = true;
        template.UpdatedAt = DateTime.UtcNow;
        templateRepository.Update(template);
        await templateRepository.SaveChangesAsync(ct);

        return MapToResponse(template);
    }

    public async Task<EmailTemplateResponse> DeactivateAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Email template {id} not found.");

        template.IsActive = false;
        template.UpdatedAt = DateTime.UtcNow;
        templateRepository.Update(template);
        await templateRepository.SaveChangesAsync(ct);

        return MapToResponse(template);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var template = await templateRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Email template {id} not found.");

        templateRepository.SoftDelete(template);
        await templateRepository.SaveChangesAsync(ct);
    }

    public Task<EmailTemplate?> GetActiveTemplateAsync(EmailTemplateType type, CancellationToken ct = default)
        => templateRepository.Query()
            .AsNoTracking()
            .FirstOrDefaultAsync(t => t.TemplateType == type && t.IsActive, ct);

    private static EmailTemplateResponse MapToResponse(EmailTemplate t) => new(
        t.Id, t.Name, t.Subject, t.HtmlBody,
        (int)t.TemplateType, t.TemplateType.ToString(),
        t.IsActive, t.Version, t.CreatedAt, t.UpdatedAt);
}
