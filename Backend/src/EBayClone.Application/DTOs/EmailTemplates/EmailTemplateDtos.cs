using EBayClone.Domain.Enums;

namespace EBayClone.Application.DTOs.EmailTemplates;

public record EmailTemplateResponse(
    Guid Id,
    string Name,
    string Subject,
    string HtmlBody,
    int TemplateType,
    string TemplateTypeName,
    bool IsActive,
    int Version,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record CreateEmailTemplateRequest(
    string Name,
    string Subject,
    string HtmlBody,
    EmailTemplateType TemplateType
);

public record UpdateEmailTemplateRequest(
    string Name,
    string Subject,
    string HtmlBody
);
