using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class UserDocument : BaseEntity
{
    public string FileName { get; set; } = string.Empty;
    public string FileUrl { get; set; } = string.Empty;
    public DocumentType DocumentType { get; set; }
    public long FileSizeBytes { get; set; }
    public string ContentType { get; set; } = string.Empty;

    public Guid BusinessProfileId { get; set; }
    public BusinessProfile BusinessProfile { get; set; } = null!;
}
