using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class AttributeOption : BaseEntity
{
    public Guid CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;

    public string Value { get; set; } = string.Empty;
    public string Label { get; set; } = string.Empty;
    public int SortOrder { get; set; }
    public bool IsActive { get; set; } = true;
}
