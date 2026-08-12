using EBayClone.Domain.Common;

namespace EBayClone.Domain.Entities;

public class Category : BaseEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public string? ImageUrl { get; set; }
    public int SortOrder { get; set; } = 0;
    public Guid? ParentCategoryId { get; set; }

    public Category? ParentCategory { get; set; }
    public ICollection<Category> SubCategories { get; set; } = new List<Category>();
    public ICollection<CategoryAttribute> Attributes { get; set; } = new List<CategoryAttribute>();
    public ICollection<Listing> Listings { get; set; } = new List<Listing>();
}
