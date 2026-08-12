using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class ListingImage : BaseEntity
{
    public string Url { get; set; } = string.Empty;
    public string? AltText { get; set; }
    public int SortOrder { get; set; } = 0;

    public Guid ListingId { get; set; }
    public Listing Listing { get; set; } = null!;
}