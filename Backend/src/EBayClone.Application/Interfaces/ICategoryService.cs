using EBayClone.Domain.Enums;

namespace EBayClone.Application.Interfaces;

public interface ICategoryService
{
    Task<IEnumerable<CategoryResponse>> GetAllAsync(CancellationToken ct = default);
    Task<IEnumerable<CategoryTreeResponse>> GetTreeAsync(CancellationToken ct = default);
    Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<CategoryMetadataResponse> GetMetadataAsync(Guid id, CancellationToken ct = default);
    Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct = default);
    Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<CategoryAttributeResponse> CreateAttributeAsync(Guid categoryId, CategoryAttributeRequest request, CancellationToken ct = default);
    Task<CategoryAttributeResponse> UpdateAttributeAsync(Guid categoryId, Guid attributeId, CategoryAttributeRequest request, CancellationToken ct = default);
    Task DeleteAttributeAsync(Guid categoryId, Guid attributeId, CancellationToken ct = default);
}

public record CategoryRequest(
    string Name,
    string? Description = null,
    Guid? ParentCategoryId = null,
    string? ImageUrl = null,
    int SortOrder = 0,
    IReadOnlyCollection<CategoryAttributeRequest>? Attributes = null,
    IReadOnlyCollection<CategoryRequest>? SubCategories = null
);

public record CategoryResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? ImageUrl,
    int SortOrder,
    IReadOnlyCollection<CategoryAttributeResponse> Attributes
);

public record CategoryTreeResponse(
    Guid Id,
    string Name,
    string? Description,
    Guid? ParentCategoryId,
    string? ImageUrl,
    int SortOrder,
    IReadOnlyCollection<CategoryAttributeResponse> Attributes,
    IReadOnlyCollection<CategoryTreeResponse> Children
);

public record CategoryMetadataResponse(
    Guid CategoryId,
    string CategoryName,
    IReadOnlyCollection<CategoryAttributeResponse> Attributes
);

public record CategoryAttributeRequest(
    string Name,
    string DisplayName,
    AttributeDataType DataType,
    bool IsRequired = false,
    bool IsFilterable = false,
    int SortOrder = 0,
    string? Description = null,
    string? Placeholder = null,
    string? Unit = null,
    int? MinLength = null,
    int? MaxLength = null,
    decimal? MinValue = null,
    decimal? MaxValue = null,
    string? RegexPattern = null,
    Guid? ConditionAttributeId = null,
    ConditionalOperator? ConditionOperator = null,
    string? ConditionValue = null,
    IReadOnlyCollection<AttributeOptionRequest>? Options = null
);

public record AttributeOptionRequest(
    string Value,
    string Label,
    int SortOrder = 0,
    bool IsActive = true
);

public record CategoryAttributeResponse(
    Guid Id,
    Guid CategoryId,
    string Name,
    string DisplayName,
    int DataType,
    bool IsRequired,
    bool IsFilterable,
    int SortOrder,
    string? Description,
    string? Placeholder,
    string? Unit,
    int? MinLength,
    int? MaxLength,
    decimal? MinValue,
    decimal? MaxValue,
    string? RegexPattern,
    Guid? ConditionAttributeId,
    int? ConditionOperator,
    string? ConditionValue,
    IReadOnlyCollection<AttributeOptionResponse> Options
);

public record AttributeOptionResponse(
    Guid Id,
    string Value,
    string Label,
    int SortOrder,
    bool IsActive
);
