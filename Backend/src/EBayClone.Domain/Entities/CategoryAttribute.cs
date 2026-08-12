using EBayClone.Domain.Common;
using EBayClone.Domain.Enums;

namespace EBayClone.Domain.Entities;

public class CategoryAttribute : BaseEntity
{
    public Guid CategoryId { get; set; }
    public Category Category { get; set; } = null!;

    public string Name { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string? Description { get; set; }
    public AttributeDataType DataType { get; set; } = AttributeDataType.Text;
    public bool IsRequired { get; set; }
    public bool IsFilterable { get; set; }
    public int SortOrder { get; set; }
    public string? Placeholder { get; set; }
    public string? Unit { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public decimal? MinValue { get; set; }
    public decimal? MaxValue { get; set; }
    public string? RegexPattern { get; set; }

    public Guid? ConditionAttributeId { get; set; }
    public CategoryAttribute? ConditionAttribute { get; set; }
    public ConditionalOperator? ConditionOperator { get; set; }
    public string? ConditionValue { get; set; }

    public ICollection<AttributeOption> Options { get; set; } = new List<AttributeOption>();
    public ICollection<ListingAttributeValue> ListingValues { get; set; } = new List<ListingAttributeValue>();
}
