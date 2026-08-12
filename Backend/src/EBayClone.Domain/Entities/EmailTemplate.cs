using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class EmailTemplate : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string HtmlBody { get; set; } = string.Empty;
    public EmailTemplateType TemplateType { get; set; }
    public bool IsActive { get; set; } = false;
    public int Version { get; set; } = 1;
}
