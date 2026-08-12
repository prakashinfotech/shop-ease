using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class ListingAttributeValue : BaseEntity
{
    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;

    public Guid CategoryAttributeId { get; set; }
    public CategoryAttribute CategoryAttribute { get; set; } = null!;

    public string Value { get; set; } = string.Empty;
}
