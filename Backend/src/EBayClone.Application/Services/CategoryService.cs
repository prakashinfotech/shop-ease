using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Application.Services;

public class CategoryService(
    IRepository<Category> categoryRepository,
    IRepository<CategoryAttribute> attributeRepository) : ICategoryService
{
    public async Task<IEnumerable<CategoryResponse>> GetAllAsync(CancellationToken ct = default)
    {
        var categories = await CategoryQuery()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return categories.Select(c => MapToResponse(c, categories));
    }

    public async Task<IEnumerable<CategoryTreeResponse>> GetTreeAsync(CancellationToken ct = default)
    {
        var categories = await CategoryQuery()
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .AsNoTracking()
            .ToListAsync(ct);

        return BuildTree(categories, null);
    }

    public async Task<CategoryResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var categories = await CategoryQuery()
            .AsNoTracking()
            .ToListAsync(ct);

        var category = categories.FirstOrDefault(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        return MapToResponse(category, categories);
    }

    public async Task<CategoryMetadataResponse> GetMetadataAsync(Guid id, CancellationToken ct = default)
    {
        var categories = await CategoryQuery()
            .AsNoTracking()
            .ToListAsync(ct);

        var category = categories.FirstOrDefault(c => c.Id == id)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        if (!ShouldExposeAttributes(categories, category))
            return new CategoryMetadataResponse(category.Id, category.Name, []);

        var attributes = category.Attributes
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.DisplayName)
            .Select(MapAttributeToResponse)
            .ToList();

        return new CategoryMetadataResponse(category.Id, category.Name, attributes);
    }

    public async Task<CategoryResponse> CreateAsync(CategoryRequest request, CancellationToken ct = default)
    {
        ValidateCategoryRequestHierarchy(request, !request.ParentCategoryId.HasValue, isCreate: true);
        await ValidateParentAsync(null, request.ParentCategoryId, ct);

        var category = new Category();
        ApplyCategoryRequest(category, request);

        AddSubCategories(category, request.SubCategories);
        AddAttributes(category, request.Attributes);

        await categoryRepository.AddAsync(category, ct);
        await categoryRepository.SaveChangesAsync(ct);

        var allCreated = FlattenCategoryTree(category);
        return MapToResponse(category, allCreated);
    }

    public async Task<CategoryResponse> UpdateAsync(Guid id, CategoryRequest request, CancellationToken ct = default)
    {
        var category = await CategoryQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        ValidateCategoryRequestHierarchy(request, !request.ParentCategoryId.HasValue && !category.ParentCategoryId.HasValue, isCreate: false);
        await ValidateParentAsync(id, request.ParentCategoryId, ct);
        ApplyCategoryRequest(category, request);

        if (request.Attributes is not null)
            ReplaceAttributes(category, request.Attributes);
        else if (!request.ParentCategoryId.HasValue)
            foreach (var attribute in category.Attributes)
                SoftDeleteAttribute(attribute);

        categoryRepository.Update(category);
        await categoryRepository.SaveChangesAsync(ct);

        return MapToResponse(category, [category]);
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var category = await CategoryQuery()
            .FirstOrDefaultAsync(c => c.Id == id, ct)
            ?? throw new KeyNotFoundException($"Category {id} not found.");

        foreach (var attribute in category.Attributes)
            SoftDeleteAttribute(attribute);

        categoryRepository.SoftDelete(category);
        await categoryRepository.SaveChangesAsync(ct);
    }

    public async Task<CategoryAttributeResponse> CreateAttributeAsync(Guid categoryId, CategoryAttributeRequest request, CancellationToken ct = default)
    {
        _ = await categoryRepository.GetByIdAsync(categoryId, ct)
            ?? throw new KeyNotFoundException($"Category {categoryId} not found.");

        var attribute = CreateAttribute(categoryId, request);
        await attributeRepository.AddAsync(attribute, ct);
        await attributeRepository.SaveChangesAsync(ct);

        return MapAttributeToResponse(attribute);
    }

    public async Task<CategoryAttributeResponse> UpdateAttributeAsync(Guid categoryId, Guid attributeId, CategoryAttributeRequest request, CancellationToken ct = default)
    {
        var attribute = await attributeRepository.Query()
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == attributeId && a.CategoryId == categoryId, ct)
            ?? throw new KeyNotFoundException($"Attribute {attributeId} not found.");

        ApplyAttributeRequest(attribute, request);
        ReplaceOptions(attribute, request.Options);
        attributeRepository.Update(attribute);
        await attributeRepository.SaveChangesAsync(ct);

        return MapAttributeToResponse(attribute);
    }

    public async Task DeleteAttributeAsync(Guid categoryId, Guid attributeId, CancellationToken ct = default)
    {
        var attribute = await attributeRepository.Query()
            .Include(a => a.Options)
            .FirstOrDefaultAsync(a => a.Id == attributeId && a.CategoryId == categoryId, ct)
            ?? throw new KeyNotFoundException($"Attribute {attributeId} not found.");

        SoftDeleteAttribute(attribute);
        attributeRepository.Update(attribute);
        await attributeRepository.SaveChangesAsync(ct);
    }

    private IQueryable<Category> CategoryQuery() =>
        categoryRepository.Query()
            .Include(c => c.Attributes.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder).ThenBy(o => o.Label));

    private static void ApplyCategoryRequest(Category category, CategoryRequest request)
    {
        category.Name = request.Name.Trim();
        category.Description = request.Description?.Trim();
        category.ParentCategoryId = request.ParentCategoryId;
        category.ImageUrl = request.ImageUrl?.Trim();
        category.SortOrder = request.SortOrder;
        category.UpdatedAt = DateTime.UtcNow;
    }

    private async Task ValidateParentAsync(Guid? categoryId, Guid? parentCategoryId, CancellationToken ct)
    {
        if (!parentCategoryId.HasValue)
            return;

        if (categoryId == parentCategoryId)
            throw new InvalidOperationException("A category cannot be its own parent.");

        var categories = await categoryRepository.Query().AsNoTracking().ToListAsync(ct);
        if (!categories.Any(c => c.Id == parentCategoryId.Value))
            throw new KeyNotFoundException("Parent category not found.");

        var currentParent = categories.FirstOrDefault(c => c.Id == parentCategoryId.Value);
        while (currentParent is not null)
        {
            if (currentParent.Id == categoryId)
                throw new InvalidOperationException("Category parent would create a cycle.");

            currentParent = currentParent.ParentCategoryId.HasValue
                ? categories.FirstOrDefault(c => c.Id == currentParent.ParentCategoryId.Value)
                : null;
        }
    }

    private static void ValidateCategoryRequestHierarchy(CategoryRequest request, bool isLevel0, bool isCreate)
    {
        if (isLevel0 && request.Attributes?.Any() == true)
            throw new InvalidOperationException("Parent categories cannot have listing form attributes. Add attributes to a child category.");

        if (isCreate && isLevel0 && (request.SubCategories is null || !request.SubCategories.Any()))
            throw new InvalidOperationException("A level 0 category must have at least one subcategory.");

        if (request.SubCategories is not null)
        {
            foreach (var sub in request.SubCategories)
            {
                ValidateCategoryRequestHierarchy(sub, false, isCreate);
            }
        }
    }

    private static void AddSubCategories(Category parent, IReadOnlyCollection<CategoryRequest>? requests)
    {
        if (requests is null || !requests.Any())
            return;

        foreach (var req in requests)
        {
            var sub = new Category();
            ApplyCategoryRequest(sub, req);
            AddSubCategories(sub, req.SubCategories);
            AddAttributes(sub, req.Attributes);
            parent.SubCategories.Add(sub);
        }
    }

    private static IReadOnlyCollection<Category> FlattenCategoryTree(Category root)
    {
        var list = new List<Category> { root };
        foreach (var sub in root.SubCategories)
        {
            list.AddRange(FlattenCategoryTree(sub));
        }
        return list;
    }

    private static void AddAttributes(Category category, IReadOnlyCollection<CategoryAttributeRequest>? requests)
    {
        if (requests is null)
            return;

        foreach (var request in requests)
            category.Attributes.Add(CreateAttribute(category.Id, request));
    }

    private static CategoryAttribute CreateAttribute(Guid categoryId, CategoryAttributeRequest request)
    {
        var attribute = new CategoryAttribute { CategoryId = categoryId };
        ApplyAttributeRequest(attribute, request);
        AddOptions(attribute, request.Options);
        return attribute;
    }

    private static void ReplaceAttributes(Category category, IReadOnlyCollection<CategoryAttributeRequest> requests)
    {
        foreach (var existing in category.Attributes)
            SoftDeleteAttribute(existing);

        foreach (var request in requests)
            category.Attributes.Add(CreateAttribute(category.Id, request));
    }

    private static void ApplyAttributeRequest(CategoryAttribute attribute, CategoryAttributeRequest request)
    {
        attribute.Name = request.Name.Trim();
        attribute.DisplayName = request.DisplayName.Trim();
        attribute.DataType = request.DataType;
        attribute.IsRequired = request.IsRequired;
        attribute.IsFilterable = request.IsFilterable;
        attribute.SortOrder = request.SortOrder;
        attribute.Description = request.Description?.Trim();
        attribute.Placeholder = request.Placeholder?.Trim();
        attribute.Unit = request.Unit?.Trim();
        attribute.MinLength = request.MinLength;
        attribute.MaxLength = request.MaxLength;
        attribute.MinValue = request.MinValue;
        attribute.MaxValue = request.MaxValue;
        attribute.RegexPattern = request.RegexPattern?.Trim();
        attribute.ConditionAttributeId = request.ConditionAttributeId;
        attribute.ConditionOperator = request.ConditionOperator;
        attribute.ConditionValue = request.ConditionValue?.Trim();
        attribute.UpdatedAt = DateTime.UtcNow;
    }

    private static void ReplaceOptions(CategoryAttribute attribute, IReadOnlyCollection<AttributeOptionRequest>? requests)
    {
        foreach (var option in attribute.Options)
        {
            option.IsDeleted = true;
            option.DeletedAt = DateTime.UtcNow;
            option.UpdatedAt = DateTime.UtcNow;
        }

        AddOptions(attribute, requests);
    }

    private static void AddOptions(CategoryAttribute attribute, IReadOnlyCollection<AttributeOptionRequest>? requests)
    {
        if (requests is null)
            return;

        foreach (var request in requests)
        {
            attribute.Options.Add(new AttributeOption
            {
                Value = request.Value.Trim(),
                Label = request.Label.Trim(),
                SortOrder = request.SortOrder,
                IsActive = request.IsActive,
            });
        }
    }

    private static void SoftDeleteAttribute(CategoryAttribute attribute)
    {
        attribute.IsDeleted = true;
        attribute.DeletedAt = DateTime.UtcNow;
        attribute.UpdatedAt = DateTime.UtcNow;

        foreach (var option in attribute.Options)
        {
            option.IsDeleted = true;
            option.DeletedAt = DateTime.UtcNow;
            option.UpdatedAt = DateTime.UtcNow;
        }
    }

    private static IReadOnlyCollection<CategoryTreeResponse> BuildTree(IReadOnlyCollection<Category> categories, Guid? parentId) =>
        categories
            .Where(c => c.ParentCategoryId == parentId)
            .OrderBy(c => c.SortOrder)
            .ThenBy(c => c.Name)
            .Select(c => new CategoryTreeResponse(
                c.Id,
                c.Name,
                c.Description,
                c.ParentCategoryId,
                c.ImageUrl,
                c.SortOrder,
                ShouldExposeAttributes(categories, c)
                    ? c.Attributes.Where(a => !a.IsDeleted).Select(MapAttributeToResponse).ToList()
                    : [],
                BuildTree(categories, c.Id)))
            .ToList();

    private static bool ShouldExposeAttributes(IReadOnlyCollection<Category> categories, Category category) =>
        category.ParentCategoryId.HasValue && !categories.Any(c => c.ParentCategoryId == category.Id);

    private static CategoryResponse MapToResponse(Category c, IReadOnlyCollection<Category> categories) =>
        new(
            c.Id,
            c.Name,
            c.Description,
            c.ParentCategoryId,
            c.ImageUrl,
            c.SortOrder,
            ShouldExposeAttributes(categories, c)
                ? c.Attributes.Where(a => !a.IsDeleted).OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName).Select(MapAttributeToResponse).ToList()
                : []);

    private static CategoryAttributeResponse MapAttributeToResponse(CategoryAttribute a) =>
        new(
            a.Id,
            a.CategoryId,
            a.Name,
            a.DisplayName,
            (int)a.DataType,
            a.IsRequired,
            a.IsFilterable,
            a.SortOrder,
            a.Description,
            a.Placeholder,
            a.Unit,
            a.MinLength,
            a.MaxLength,
            a.MinValue,
            a.MaxValue,
            a.RegexPattern,
            a.ConditionAttributeId,
            a.ConditionOperator.HasValue ? (int)a.ConditionOperator.Value : null,
            a.ConditionValue,
            a.Options
                .Where(o => !o.IsDeleted)
                .OrderBy(o => o.SortOrder)
                .ThenBy(o => o.Label)
                .Select(o => new AttributeOptionResponse(o.Id, o.Value, o.Label, o.SortOrder, o.IsActive))
                .ToList());
}
