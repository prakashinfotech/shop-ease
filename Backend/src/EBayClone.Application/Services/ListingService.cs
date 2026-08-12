using System.Text.Json;
using System.Text.RegularExpressions;
using EBayClone.Application.Common;
using EBayClone.Application.DTOs.Listings;
using EBayClone.Application.Interfaces;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using EBayClone.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace EBayClone.Application.Services;

public class ListingService(
    IRepository<Listing> listingRepository,
    IRepository<User> userRepository,
    IRepository<Category> categoryRepository,
    IRepository<ListingImage> listingImageRepository,
    IRepository<ListingAttributeValue> listingAttributeValueRepository,
    IRepository<ListingView> listingViewRepository,
    IRepository<ListingVersion> listingVersionRepository,
    IRepository<ListingApprovalLog> listingApprovalLogRepository,
    ILogger<ListingService> logger) : IListingService
{
    private static readonly JsonSerializerOptions JsonOpts =
        new() { PropertyNamingPolicy = JsonNamingPolicy.CamelCase };

    private static void ValidateSellerCanSell(User seller)
    {
        if (!seller.IsEmailVerified)
            throw new InvalidOperationException("Please verify your email before selling items.");

        if (seller.AccountType == AccountType.Business)
        {
            if (seller.BusinessProfile is null)
                throw new InvalidOperationException("Please complete your business profile before selling items.");

            if (seller.BusinessProfile.VerificationStatus != VerificationStatus.Verified)
                throw new InvalidOperationException("Your business profile must be verified by admin before you can create listings.");
        }
    }

    public async Task<PagedResult<ListingResponse>> GetListingsAsync(ListingQuery query, CancellationToken ct = default)
    {
        var q = ListingQueryBase(query.IncludeDeleted)
            .AsNoTracking();

        if (query.Status.HasValue)
            q = q.Where(l => l.Status == query.Status.Value);

        if (query.ListingType.HasValue)
            q = q.Where(l => l.ListingType == query.ListingType.Value);

        if (!string.IsNullOrWhiteSpace(query.Search))
            q = q.Where(l => l.Title.Contains(query.Search) || l.Description.Contains(query.Search));

        if (query.CategoryId.HasValue)
        {
            var categoryIds = await GetCategoryAndDescendantIdsAsync(query.CategoryId.Value, ct);
            q = FilterByCategoryScope(q, categoryIds);
        }
        else if (!string.IsNullOrWhiteSpace(query.Category))
        {
            var categoryIds = await GetCategoryAndDescendantIdsByNameAsync(query.Category, ct);
            q = FilterByCategoryScope(q, categoryIds);
        }

        if (query.MinPrice.HasValue)
            q = q.Where(l => l.Price - l.DiscountAmount >= query.MinPrice.Value);

        if (query.MaxPrice.HasValue)
            q = q.Where(l => l.Price - l.DiscountAmount <= query.MaxPrice.Value);

        if (query.FreeShipping == true)
            q = q.Where(l => l.FreeShipping);

        if (query.SellerId.HasValue)
            q = q.Where(l => l.SellerId == query.SellerId.Value);

        if (query.ExcludeSellerId.HasValue)
            q = q.Where(l => l.SellerId != query.ExcludeSellerId.Value);

        if (query.AttributeFilters?.Count > 0)
        {
            foreach (var (attrIdStr, value) in query.AttributeFilters)
            {
                if (!Guid.TryParse(attrIdStr, out var attrId) || string.IsNullOrWhiteSpace(value))
                    continue;
                q = q.Where(l => l.AttributeValues.Any(
                    v => !v.IsDeleted && v.CategoryAttributeId == attrId && v.Value == value));
            }
        }

        q = query.SortBy?.ToLower() switch
        {
            "price" => query.SortDirection == "asc"
                ? q.OrderBy(l => l.Price - l.DiscountAmount)
                : q.OrderByDescending(l => l.Price - l.DiscountAmount),
            "title" => query.SortDirection == "asc" ? q.OrderBy(l => l.Title) : q.OrderByDescending(l => l.Title),
            "createdat" => query.SortDirection == "asc" ? q.OrderBy(l => l.CreatedAt) : q.OrderByDescending(l => l.CreatedAt),
            "updatedat" => query.SortDirection == "asc" ? q.OrderBy(l => l.UpdatedAt) : q.OrderByDescending(l => l.UpdatedAt),
            _ => query.SortDirection == "asc" ? q.OrderBy(l => l.UpdatedAt) : q.OrderByDescending(l => l.UpdatedAt),
        };

        var total = await q.CountAsync(ct);
        var items = await q
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .ToListAsync(ct);

        return PagedResult<ListingResponse>.Create(
            items.Select(MapToResponse).ToList(),
            total, query.Page, query.PageSize);
    }

    public async Task<ListingResponse> GetByIdAsync(Guid id, CancellationToken ct = default)
    {
        var listing = await ListingQueryBase(includeDeleted: false)
            .AsNoTracking()
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        return MapToResponse(listing);
    }

    public async Task<PagedResult<ListingResponse>> GetMyListingsAsync(Guid sellerId, ListingQuery query, CancellationToken ct = default)
    {
        return await GetListingsAsync(query with { SellerId = sellerId }, ct);
    }

    public async Task<ListingResponse> CreateAsync(Guid sellerId, CreateListingRequest request, CancellationToken ct = default)
    {
        var seller = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == sellerId, ct)
            ?? throw new KeyNotFoundException("Seller not found.");

        ValidateSellerCanSell(seller);

        var metadata = request.CategoryId.HasValue
            ? await GetCategoryAttributesAsync(request.CategoryId.Value, "created", ct)
            : [];

        ValidateListingRequest(request, metadata);

        var listing = new Listing
        {
            SellerId = sellerId,
            Seller = seller,
        };

        ApplyListingRequest(listing, request);
        ApplyAttributeValues(listing, request.AttributeValues, metadata);
        ApplyImages(listing, request.Images);

        await listingRepository.AddAsync(listing, ct);
        await listingRepository.SaveChangesAsync(ct);

        var version = new ListingVersion
        {
            ListingId = listing.Id,
            VersionNumber = 1,
            SnapshotJson = JsonSerializer.Serialize(BuildSnapshot(listing), JsonOpts),
            IsPendingUpdate = false,
            Status = ListingVersionStatus.PendingApproval,
            SubmittedAt = DateTime.UtcNow,
        };

        await listingVersionRepository.AddAsync(version, ct);
        await AddSubmissionLogAsync(listing.Id, version.Id, sellerId, "Listing submitted for approval.", ct);
        await listingVersionRepository.SaveChangesAsync(ct);

        logger.LogInformation("Listing created: {Id} by seller {SellerId}", listing.Id, sellerId);

        return await GetByIdAsync(listing.Id, ct);
    }

    public async Task<ListingResponse> UpdateAsync(Guid id, Guid sellerId, UpdateListingRequest request, CancellationToken ct = default)
    {
        var listing = await ListingQueryBase(includeDeleted: false)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only edit your own listings.");

        var seller = await userRepository.Query()
            .Include(u => u.BusinessProfile)
            .FirstOrDefaultAsync(u => u.Id == sellerId, ct)
            ?? throw new KeyNotFoundException("Seller not found.");

        ValidateSellerCanSell(seller);

        var metadata = request.CategoryId.HasValue
            ? await GetCategoryAttributesAsync(request.CategoryId.Value, "updated", ct)
            : [];

        ValidateListingRequest(request, metadata);

        if (listing.Status == ListingStatus.Active)
        {
            if (listing.HasPendingVersion)
                throw new InvalidOperationException("This listing already has a pending update under review.");

            var version = new ListingVersion
            {
                ListingId = listing.Id,
                VersionNumber = await NextVersionNumberAsync(listing.Id, ct),
                SnapshotJson = JsonSerializer.Serialize(BuildSnapshot(request, listing), JsonOpts),
                IsPendingUpdate = true,
                Status = ListingVersionStatus.PendingApproval,
                SubmittedAt = DateTime.UtcNow,
            };

            await listingVersionRepository.AddAsync(version, ct);
            await AddSubmissionLogAsync(listing.Id, version.Id, sellerId, "Listing update submitted for approval.", ct);

            listing.HasPendingVersion = true;
            listing.UpdatedAt = DateTime.UtcNow;
            await listingRepository.SaveChangesAsync(ct);

            logger.LogInformation("Listing update submitted for approval: {Id} by seller {SellerId}", listing.Id, sellerId);

            return await GetByIdAsync(listing.Id, ct);
        }

        ApplyListingRequest(listing, request);
        listing.Status = ListingStatus.PendingApproval;
        listing.HasPendingVersion = false;
        await ReplaceAttributeValuesAsync(listing, request.AttributeValues, metadata, ct);
        await ReplaceImagesAsync(listing, request.Images, ct);
        listing.UpdatedAt = DateTime.UtcNow;

        await listingRepository.SaveChangesAsync(ct);

        var pendingVersion = await listingVersionRepository.Query()
            .FirstOrDefaultAsync(v =>
                v.ListingId == listing.Id &&
                v.Status == ListingVersionStatus.PendingApproval &&
                !v.IsPendingUpdate,
                ct);

        if (pendingVersion is null)
        {
            pendingVersion = new ListingVersion
            {
                ListingId = listing.Id,
                VersionNumber = await NextVersionNumberAsync(listing.Id, ct),
                SnapshotJson = JsonSerializer.Serialize(BuildSnapshot(listing), JsonOpts),
                IsPendingUpdate = false,
                Status = ListingVersionStatus.PendingApproval,
                SubmittedAt = DateTime.UtcNow,
            };

            await listingVersionRepository.AddAsync(pendingVersion, ct);
        }
        else
        {
            pendingVersion.SnapshotJson = JsonSerializer.Serialize(BuildSnapshot(listing), JsonOpts);
            pendingVersion.SubmittedAt = DateTime.UtcNow;
            pendingVersion.RejectionReason = null;
            pendingVersion.ReviewedAt = null;
            pendingVersion.ReviewedByAdminId = null;
            listingVersionRepository.Update(pendingVersion);
        }

        await AddSubmissionLogAsync(listing.Id, pendingVersion.Id, sellerId, "Listing submitted for approval.", ct);
        await listingVersionRepository.SaveChangesAsync(ct);

        return await GetByIdAsync(listing.Id, ct);
    }

    public async Task DeleteAsync(Guid id, Guid sellerId, CancellationToken ct = default)
    {
        var listing = await listingRepository.GetByIdAsync(id, ct)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only delete your own listings.");

        listingRepository.SoftDelete(listing);
        await listingRepository.SaveChangesAsync(ct);
    }

    public async Task<ListingResponse> RestoreAsync(Guid id, Guid sellerId, CancellationToken ct = default)
    {
        var listing = await ListingQueryBase(includeDeleted: true)
            .FirstOrDefaultAsync(l => l.Id == id, ct)
            ?? throw new KeyNotFoundException($"Listing {id} not found.");

        if (listing.SellerId != sellerId)
            throw new UnauthorizedAccessException("You can only restore your own listings.");

        listing.IsDeleted = false;
        listing.DeletedAt = null;
        listing.UpdatedAt = DateTime.UtcNow;
        listingRepository.Update(listing);
        await listingRepository.SaveChangesAsync(ct);

        return MapToResponse(listing);
    }

    public async Task RecordViewAsync(Guid userId, Guid listingId, CancellationToken ct = default)
    {
        var listing = await listingRepository.Query()
            .AsNoTracking()
            .Where(l => l.Id == listingId && l.SellerId != userId)
            .Select(l => new { l.Id })
            .FirstOrDefaultAsync(ct);

        if (listing is null)
            return;

        var now = DateTime.UtcNow;
        var existing = await listingViewRepository.Query()
            .FirstOrDefaultAsync(v => v.UserId == userId && v.ListingId == listingId, ct);

        if (existing is null)
        {
            await listingViewRepository.AddAsync(new ListingView
            {
                UserId = userId,
                ListingId = listingId,
                LastViewedAt = now,
            }, ct);
        }
        else
        {
            existing.LastViewedAt = now;
            existing.UpdatedAt = now;
        }

        await listingViewRepository.SaveChangesAsync(ct);
    }

    public async Task<IReadOnlyList<ListingResponse>> GetRecentlyViewedAsync(
        Guid userId,
        int days = 3,
        int take = 12,
        CancellationToken ct = default)
    {
        var cutoff = DateTime.UtcNow.AddDays(-Math.Max(1, days));
        take = Math.Clamp(take, 1, 24);

        var recentViews = await listingViewRepository.Query()
            .AsNoTracking()
            .Where(v => v.UserId == userId && v.LastViewedAt >= cutoff)
            .OrderByDescending(v => v.LastViewedAt)
            .Take(take)
            .Select(v => new { v.ListingId, v.LastViewedAt })
            .ToListAsync(ct);

        if (recentViews.Count == 0)
            return [];

        var orderedIds = recentViews.Select(v => v.ListingId).ToList();
        var rank = orderedIds.Select((id, index) => new { id, index })
            .ToDictionary(x => x.id, x => x.index);

        var listings = await ListingQueryBase(includeDeleted: false)
            .AsNoTracking()
            .Where(l => orderedIds.Contains(l.Id) &&
                l.SellerId != userId &&
                l.Status == ListingStatus.Active)
            .ToListAsync(ct);

        return listings
            .OrderBy(l => rank.GetValueOrDefault(l.Id, int.MaxValue))
            .Select(MapToResponse)
            .ToList();
    }

    public async Task<AutocompleteResponse> GetAutocompleteSuggestionsAsync(string q, CancellationToken ct = default)
    {
        if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
            return new AutocompleteResponse([]);

        var suggestions = await listingRepository.Query()
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active && l.Title.Contains(q))
            .Select(l => l.Title)
            .Distinct()
            .Take(8)
            .ToListAsync(ct);

        return new AutocompleteResponse(suggestions);
    }

    public async Task<SearchFacetsResponse> GetSearchFacetsAsync(Guid? categoryId, string? search, CancellationToken ct = default)
    {
        var q = listingRepository.Query()
            .AsNoTracking()
            .Where(l => l.Status == ListingStatus.Active);

        if (!string.IsNullOrWhiteSpace(search))
            q = q.Where(l => l.Title.Contains(search) || l.Description.Contains(search));

        IReadOnlyCollection<Guid> categoryScopeIds = [];
        if (categoryId.HasValue)
        {
            categoryScopeIds = await GetCategoryAndDescendantIdsAsync(categoryId.Value, ct);
            q = FilterByCategoryScope(q, categoryScopeIds);
        }

        var priceStats = await q
            .GroupBy(_ => 1)
            .Select(g => new { Min = g.Min(l => l.Price - l.DiscountAmount), Max = g.Max(l => l.Price - l.DiscountAmount) })
            .FirstOrDefaultAsync(ct);

        var priceFacet = priceStats != null
            ? new PriceFacet(priceStats.Min, priceStats.Max)
            : null;

        List<AttributeFacet> attributeFacets = [];

        if (categoryId.HasValue)
        {
            var category = await categoryRepository.Query()
                .Include(c => c.Attributes.OrderBy(a => a.SortOrder))
                    .ThenInclude(a => a.Options.Where(o => o.IsActive).OrderBy(o => o.SortOrder))
                .AsNoTracking()
                .FirstOrDefaultAsync(c => c.Id == categoryId.Value, ct);

            if (category != null)
            {
                var filterableAttrs = category.Attributes
                    .Where(a => a.DataType is AttributeDataType.Dropdown
                        or AttributeDataType.MultiSelect
                        or AttributeDataType.Boolean)
                    .ToList();

                var filterableAttrIds = filterableAttrs.Select(a => a.Id).ToHashSet();

                var allValueCountsQuery = listingRepository.Query()
                    .AsNoTracking()
                    .Where(l => l.Status == ListingStatus.Active);

                allValueCountsQuery = FilterByCategoryScope(allValueCountsQuery, categoryScopeIds);

                var allValueCounts = await allValueCountsQuery
                    .SelectMany(l => l.AttributeValues
                        .Where(v => !v.IsDeleted && filterableAttrIds.Contains(v.CategoryAttributeId)))
                    .GroupBy(v => new { v.CategoryAttributeId, v.Value })
                    .Select(g => new { g.Key.CategoryAttributeId, g.Key.Value, Count = g.Count() })
                    .ToListAsync(ct);

                var countLookup = allValueCounts
                    .GroupBy(x => x.CategoryAttributeId)
                    .ToDictionary(g => g.Key, g => g.ToDictionary(x => x.Value, x => x.Count));

                foreach (var attr in filterableAttrs)
                {
                    countLookup.TryGetValue(attr.Id, out var attrCounts);
                    attrCounts ??= [];

                    IReadOnlyCollection<FacetOption> options;

                    if (attr.DataType == AttributeDataType.Boolean)
                    {
                        options = new[]
                        {
                            new FacetOption("true", "Yes", attrCounts.GetValueOrDefault("true") + attrCounts.GetValueOrDefault("True")),
                            new FacetOption("false", "No", attrCounts.GetValueOrDefault("false") + attrCounts.GetValueOrDefault("False")),
                        }.Where(o => o.Count > 0).ToList();
                    }
                    else
                    {
                        options = attr.Options
                            .Select(o => new FacetOption(o.Value, o.Label, attrCounts.GetValueOrDefault(o.Value)))
                            .Where(o => o.Count > 0)
                            .ToList();
                    }

                    if (options.Count > 0)
                        attributeFacets.Add(new AttributeFacet(attr.Id, attr.Name, attr.DisplayName, (int)attr.DataType, options));
                }
            }
        }

        return new SearchFacetsResponse(attributeFacets, priceFacet);
    }

    private IQueryable<Listing> ListingQueryBase(bool includeDeleted)
    {
        var q = listingRepository.Query();
        if (includeDeleted)
            q = q.IgnoreQueryFilters();

        return q
            .Include(l => l.Seller)
            .Include(l => l.Category)
            .Include(l => l.Images.OrderBy(i => i.SortOrder))
            .Include(l => l.AttributeValues)
                .ThenInclude(v => v.CategoryAttribute)
                    .ThenInclude(a => a.Options);
    }

    private async Task<IReadOnlyCollection<Guid>> GetCategoryAndDescendantIdsAsync(Guid categoryId, CancellationToken ct)
    {
        var categories = await GetCategoryLookupAsync(ct);
        return ExpandCategoryScope(categories, [categoryId]);
    }

    private async Task<IReadOnlyCollection<Guid>> GetCategoryAndDescendantIdsByNameAsync(string categoryName, CancellationToken ct)
    {
        var trimmed = categoryName.Trim();
        var categories = await GetCategoryLookupAsync(ct);
        var matchingIds = categories
            .Where(c => c.Name.Contains(trimmed, StringComparison.OrdinalIgnoreCase))
            .Select(c => c.Id)
            .ToList();

        return ExpandCategoryScope(categories, matchingIds);
    }

    private async Task<List<CategoryLookupItem>> GetCategoryLookupAsync(CancellationToken ct)
    {
        return await categoryRepository.Query()
            .AsNoTracking()
            .Select(c => new CategoryLookupItem(c.Id, c.ParentCategoryId, c.Name))
            .ToListAsync(ct);
    }

    private static IReadOnlyCollection<Guid> ExpandCategoryScope(
        IReadOnlyCollection<CategoryLookupItem> categories,
        IEnumerable<Guid> rootIds)
    {
        var result = new HashSet<Guid>();
        var queue = new Queue<Guid>();

        foreach (var rootId in rootIds)
        {
            if (categories.Any(c => c.Id == rootId) && result.Add(rootId))
                queue.Enqueue(rootId);
        }

        while (queue.Count > 0)
        {
            var currentId = queue.Dequeue();
            foreach (var child in categories.Where(c => c.ParentCategoryId == currentId))
            {
                if (result.Add(child.Id))
                    queue.Enqueue(child.Id);
            }
        }

        return result.ToList();
    }

    private static IQueryable<Listing> FilterByCategoryScope(IQueryable<Listing> query, IReadOnlyCollection<Guid> categoryIds)
    {
        return categoryIds.Count == 0
            ? query.Where(_ => false)
            : query.Where(l => l.CategoryId.HasValue && categoryIds.Contains(l.CategoryId.Value));
    }

    private sealed record CategoryLookupItem(Guid Id, Guid? ParentCategoryId, string Name);

    private async Task<IReadOnlyCollection<CategoryAttribute>> GetCategoryAttributesAsync(
        Guid categoryId,
        string operation,
        CancellationToken ct)
    {
        var category = await categoryRepository.Query()
            .Include(c => c.Attributes.OrderBy(a => a.SortOrder).ThenBy(a => a.DisplayName))
                .ThenInclude(a => a.Options.OrderBy(o => o.SortOrder).ThenBy(o => o.Label))
            .AsNoTracking()
            .FirstOrDefaultAsync(c => c.Id == categoryId, ct)
            ?? throw new KeyNotFoundException($"Category {categoryId} not found.");

        var hasChildren = await categoryRepository.Query()
            .AsNoTracking()
            .AnyAsync(c => c.ParentCategoryId == category.Id, ct);

        if (!category.ParentCategoryId.HasValue || hasChildren)
            throw new InvalidOperationException(
                $"Listings must be {operation} in a final child category that has its own form. Please select a subcategory, not a parent category.");

        return category.Attributes
            .Where(a => !a.IsDeleted)
            .OrderBy(a => a.SortOrder)
            .ThenBy(a => a.DisplayName)
            .ToList();
    }

    private static void ValidateListingRequest(CreateListingRequest request, IReadOnlyCollection<CategoryAttribute> metadata)
    {
        ValidateListingBasics(request.ListingType, request.Price, request.Quantity, request.DiscountAmount, request.StartingBid, request.BuyItNowPrice, request.AuctionEndAt);
        ValidateAttributeValues(request.AttributeValues, metadata);
    }

    private static void ValidateListingRequest(UpdateListingRequest request, IReadOnlyCollection<CategoryAttribute> metadata)
    {
        ValidateListingBasics(request.ListingType, request.Price, request.Quantity, request.DiscountAmount, request.StartingBid, request.BuyItNowPrice, request.AuctionEndAt);
        ValidateAttributeValues(request.AttributeValues, metadata);
    }

    private static void ValidateListingBasics(
        ListingType listingType,
        decimal price,
        int quantity,
        decimal discountAmount,
        decimal? startingBid,
        decimal? buyItNowPrice,
        DateTime? auctionEndAt)
    {
        if (listingType == ListingType.FixedPrice && price <= 0)
            throw new InvalidOperationException("Fixed price listings require a price greater than zero.");

        if (listingType == ListingType.Auction)
        {
            if (!startingBid.HasValue || startingBid.Value <= 0)
                throw new InvalidOperationException("Auction listings require a starting bid greater than zero.");

            if (auctionEndAt.HasValue && auctionEndAt.Value <= DateTime.UtcNow)
                throw new InvalidOperationException("Auction end time must be in the future.");
        }

        var effectivePrice = listingType == ListingType.Auction
            ? buyItNowPrice ?? startingBid ?? 0
            : price;

        if (discountAmount < 0)
            throw new InvalidOperationException("Discount amount cannot be negative.");

        if (discountAmount >= effectivePrice)
            throw new InvalidOperationException("Discount amount must be less than the listing price.");

        if (quantity < 1)
            throw new InvalidOperationException("Quantity must be at least 1.");
    }

    private static void ValidateAttributeValues(
        IReadOnlyCollection<ListingAttributeValueRequest>? values,
        IReadOnlyCollection<CategoryAttribute> metadata)
    {
        var submitted = (values ?? [])
            .Where(v => v.CategoryAttributeId != Guid.Empty)
            .GroupBy(v => v.CategoryAttributeId)
            .ToDictionary(g => g.Key, g => g.Last().Value?.Trim());

        var metadataById = metadata.ToDictionary(a => a.Id);

        foreach (var id in submitted.Keys)
        {
            if (!metadataById.ContainsKey(id))
                throw new InvalidOperationException("One or more attribute values do not belong to the selected category.");
        }

        foreach (var attribute in metadata)
        {
            if (!IsConditionMet(attribute, submitted))
                continue;

            submitted.TryGetValue(attribute.Id, out var value);
            var hasValue = !string.IsNullOrWhiteSpace(value);

            if (attribute.IsRequired && !hasValue)
                throw new InvalidOperationException($"{attribute.DisplayName} is required.");

            if (!hasValue)
                continue;

            ValidateAttributeValue(attribute, value!);
        }
    }

    private static bool IsConditionMet(CategoryAttribute attribute, IReadOnlyDictionary<Guid, string?> submitted)
    {
        if (!attribute.ConditionAttributeId.HasValue)
            return true;

        if (!submitted.TryGetValue(attribute.ConditionAttributeId.Value, out var actual) || string.IsNullOrWhiteSpace(actual))
            return false;

        var expected = attribute.ConditionValue ?? string.Empty;
        return attribute.ConditionOperator switch
        {
            ConditionalOperator.NotEquals => !string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
            ConditionalOperator.Contains => actual.Contains(expected, StringComparison.OrdinalIgnoreCase),
            ConditionalOperator.GreaterThan => decimal.TryParse(actual, out var a) && decimal.TryParse(expected, out var e) && a > e,
            ConditionalOperator.LessThan => decimal.TryParse(actual, out var a) && decimal.TryParse(expected, out var e) && a < e,
            _ => string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase),
        };
    }

    private static void ValidateAttributeValue(CategoryAttribute attribute, string value)
    {
        if (attribute.MinLength.HasValue && value.Length < attribute.MinLength.Value)
            throw new InvalidOperationException($"{attribute.DisplayName} must be at least {attribute.MinLength.Value} characters.");

        if (attribute.MaxLength.HasValue && value.Length > attribute.MaxLength.Value)
            throw new InvalidOperationException($"{attribute.DisplayName} must be at most {attribute.MaxLength.Value} characters.");

        if (!string.IsNullOrWhiteSpace(attribute.RegexPattern) && !Regex.IsMatch(value, attribute.RegexPattern))
            throw new InvalidOperationException($"{attribute.DisplayName} is invalid.");

        if (attribute.DataType is AttributeDataType.Number or AttributeDataType.Decimal)
        {
            if (!decimal.TryParse(value, out var number))
                throw new InvalidOperationException($"{attribute.DisplayName} must be a number.");

            if (attribute.MinValue.HasValue && number < attribute.MinValue.Value)
                throw new InvalidOperationException($"{attribute.DisplayName} must be at least {attribute.MinValue.Value}.");

            if (attribute.MaxValue.HasValue && number > attribute.MaxValue.Value)
                throw new InvalidOperationException($"{attribute.DisplayName} must be at most {attribute.MaxValue.Value}.");
        }

        if (attribute.DataType == AttributeDataType.Boolean && !bool.TryParse(value, out _))
            throw new InvalidOperationException($"{attribute.DisplayName} must be true or false.");

        if (attribute.DataType == AttributeDataType.Date && !DateTime.TryParse(value, out _))
            throw new InvalidOperationException($"{attribute.DisplayName} must be a valid date.");

        if (attribute.DataType == AttributeDataType.Dropdown)
        {
            var valid = attribute.Options.Where(o => o.IsActive).Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            if (!valid.Contains(value))
                throw new InvalidOperationException($"{attribute.DisplayName} must use one of the configured options.");
        }

        if (attribute.DataType == AttributeDataType.MultiSelect)
        {
            var valid = attribute.Options.Where(o => o.IsActive).Select(o => o.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var selected = ParseMultiSelect(value);

            if (selected.Count == 0 || selected.Any(item => !valid.Contains(item)))
                throw new InvalidOperationException($"{attribute.DisplayName} contains an invalid option.");
        }
    }

    private static IReadOnlyCollection<string> ParseMultiSelect(string value)
    {
        try
        {
            return JsonSerializer.Deserialize<IReadOnlyCollection<string>>(value) ?? [];
        }
        catch (JsonException)
        {
            return value.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        }
    }

    private static void ApplyListingRequest(Listing listing, CreateListingRequest request)
    {
        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.ListingType = request.ListingType;
        listing.Price = request.ListingType == ListingType.Auction ? request.BuyItNowPrice ?? request.StartingBid!.Value : request.Price;
        listing.DiscountAmount = request.DiscountAmount;
        listing.StartingBid = request.ListingType == ListingType.Auction ? request.StartingBid : null;
        listing.ReservePrice = request.ListingType == ListingType.Auction ? request.ReservePrice : null;
        listing.BuyItNowPrice = request.ListingType == ListingType.Auction ? request.BuyItNowPrice : null;
        listing.AuctionStartAt = request.ListingType == ListingType.Auction ? request.AuctionStartAt ?? DateTime.UtcNow : null;
        listing.AuctionEndAt = request.ListingType == ListingType.Auction ? request.AuctionEndAt : null;
        listing.Quantity = request.Quantity;
        listing.FreeShipping = request.FreeShipping;
        listing.Status = ListingStatus.PendingApproval;
        listing.CategoryId = request.CategoryId;
        listing.UpdatedAt = DateTime.UtcNow;
    }

    private static void ApplyListingRequest(Listing listing, UpdateListingRequest request)
    {
        listing.Title = request.Title.Trim();
        listing.Description = request.Description.Trim();
        listing.ListingType = request.ListingType;
        listing.Price = request.ListingType == ListingType.Auction ? request.BuyItNowPrice ?? request.StartingBid!.Value : request.Price;
        listing.DiscountAmount = request.DiscountAmount;
        listing.StartingBid = request.ListingType == ListingType.Auction ? request.StartingBid : null;
        listing.ReservePrice = request.ListingType == ListingType.Auction ? request.ReservePrice : null;
        listing.BuyItNowPrice = request.ListingType == ListingType.Auction ? request.BuyItNowPrice : null;
        listing.AuctionStartAt = request.ListingType == ListingType.Auction ? request.AuctionStartAt ?? listing.AuctionStartAt ?? DateTime.UtcNow : null;
        listing.AuctionEndAt = request.ListingType == ListingType.Auction ? request.AuctionEndAt : null;
        listing.Quantity = request.Quantity;
        listing.FreeShipping = request.FreeShipping;
        listing.CategoryId = request.CategoryId;
        listing.UpdatedAt = DateTime.UtcNow;

        if (request.Status.HasValue)
            listing.Status = request.Status.Value;
    }

    private async Task ReplaceAttributeValuesAsync(
        Listing listing,
        IReadOnlyCollection<ListingAttributeValueRequest>? requests,
        IReadOnlyCollection<CategoryAttribute> metadata,
        CancellationToken ct)
    {
        var validIds = metadata.Select(a => a.Id).ToHashSet();
        var requestedValues = (requests ?? [])
            .Where(r => validIds.Contains(r.CategoryAttributeId) && !string.IsNullOrWhiteSpace(r.Value))
            .GroupBy(r => r.CategoryAttributeId)
            .ToDictionary(g => g.Key, g => g.Last().Value!.Trim());

        foreach (var value in listing.AttributeValues.Where(v => !v.IsDeleted))
        {
            if (requestedValues.TryGetValue(value.CategoryAttributeId, out var requestedValue))
            {
                value.Value = requestedValue;
                value.DeletedAt = null;
            }
            else
            {
                value.IsDeleted = true;
                value.DeletedAt = DateTime.UtcNow;
            }

            value.UpdatedAt = DateTime.UtcNow;
        }

        var existingIds = listing.AttributeValues
            .Where(v => !v.IsDeleted)
            .Select(v => v.CategoryAttributeId)
            .ToHashSet();

        foreach (var (attributeId, requestedValue) in requestedValues)
        {
            if (existingIds.Contains(attributeId))
                continue;

            var attributeValue = new ListingAttributeValue
            {
                ListingId = listing.Id,
                CategoryAttributeId = attributeId,
                Value = requestedValue,
            };

            await listingAttributeValueRepository.AddAsync(attributeValue, ct);
        }
    }

    private static void ApplyAttributeValues(
        Listing listing,
        IReadOnlyCollection<ListingAttributeValueRequest>? requests,
        IReadOnlyCollection<CategoryAttribute> metadata)
    {
        var validIds = metadata.Select(a => a.Id).ToHashSet();
        foreach (var request in requests ?? [])
        {
            if (!validIds.Contains(request.CategoryAttributeId) || string.IsNullOrWhiteSpace(request.Value))
                continue;

            listing.AttributeValues.Add(new ListingAttributeValue
            {
                ListingId = listing.Id,
                CategoryAttributeId = request.CategoryAttributeId,
                Value = request.Value.Trim(),
            });
        }
    }

    private async Task ReplaceImagesAsync(
        Listing listing,
        IReadOnlyCollection<ListingImageRequest>? requests,
        CancellationToken ct)
    {
        var requestedImages = (requests ?? [])
            .Where(r => !string.IsNullOrWhiteSpace(r.Url))
            .Select((request, index) => new
            {
                Url = request.Url.Trim(),
                AltText = request.AltText?.Trim(),
                SortOrder = request.SortOrder,
                Index = index,
            })
            .GroupBy(r => r.Url, StringComparer.OrdinalIgnoreCase)
            .Select(g => g.Last())
            .OrderBy(r => r.SortOrder)
            .ThenBy(r => r.Index)
            .ToList();

        var existingByUrl = listing.Images
            .Where(i => !i.IsDeleted)
            .GroupBy(i => i.Url, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.First(), StringComparer.OrdinalIgnoreCase);

        var requestedUrls = requestedImages.Select(i => i.Url).ToHashSet(StringComparer.OrdinalIgnoreCase);

        foreach (var image in listing.Images.Where(i => !i.IsDeleted))
        {
            if (requestedUrls.Contains(image.Url))
                continue;

            image.IsDeleted = true;
            image.DeletedAt = DateTime.UtcNow;
            image.UpdatedAt = DateTime.UtcNow;
        }

        foreach (var request in requestedImages)
        {
            if (existingByUrl.TryGetValue(request.Url, out var existing))
            {
                existing.AltText = request.AltText;
                existing.SortOrder = request.SortOrder;
                existing.UpdatedAt = DateTime.UtcNow;
            }
            else
            {
                var image = new ListingImage
                {
                    ListingId = listing.Id,
                    Url = request.Url,
                    AltText = request.AltText,
                    SortOrder = request.SortOrder,
                };

                await listingImageRepository.AddAsync(image, ct);
            }
        }

        listing.PrimaryImageUrl = requestedImages
            .OrderBy(i => i.SortOrder)
            .ThenBy(i => i.Index)
            .Select(i => i.Url)
            .FirstOrDefault();
    }

    private static void ApplyImages(Listing listing, IReadOnlyCollection<ListingImageRequest>? requests)
    {
        foreach (var request in requests ?? [])
        {
            if (string.IsNullOrWhiteSpace(request.Url))
                continue;

            listing.Images.Add(new ListingImage
            {
                ListingId = listing.Id,
                Url = request.Url.Trim(),
                AltText = request.AltText?.Trim(),
                SortOrder = request.SortOrder,
            });
        }

        listing.PrimaryImageUrl = listing.Images
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.SortOrder)
            .Select(i => i.Url)
            .FirstOrDefault();
    }

    private async Task<int> NextVersionNumberAsync(Guid listingId, CancellationToken ct)
    {
        var max = await listingVersionRepository.Query()
            .IgnoreQueryFilters()
            .Where(v => v.ListingId == listingId)
            .Select(v => (int?)v.VersionNumber)
            .MaxAsync(ct);

        return (max ?? 0) + 1;
    }

    private async Task AddSubmissionLogAsync(Guid listingId, Guid versionId, Guid actorId, string notes, CancellationToken ct)
    {
        await listingApprovalLogRepository.AddAsync(new ListingApprovalLog
        {
            ListingId = listingId,
            VersionId = versionId,
            AdminId = actorId,
            Action = ApprovalAction.Submitted,
            Notes = notes,
        }, ct);
    }

    private static ListingSnapshotData BuildSnapshot(Listing listing) => new(
        listing.Title,
        listing.Description,
        (int)listing.ListingType,
        listing.Price,
        listing.DiscountAmount,
        listing.StartingBid,
        listing.ReservePrice,
        listing.BuyItNowPrice,
        listing.AuctionStartAt,
        listing.AuctionEndAt,
        listing.Quantity,
        listing.FreeShipping,
        listing.CategoryId,
        listing.AttributeValues
            .Where(v => !v.IsDeleted)
            .Select(v => new AttributeEntry(v.CategoryAttributeId, v.Value))
            .ToList(),
        listing.Images
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ImageEntry(i.Url, i.AltText, i.SortOrder))
            .ToList()
    );

    private static ListingSnapshotData BuildSnapshot(UpdateListingRequest request, Listing currentListing) => new(
        request.Title.Trim(),
        request.Description.Trim(),
        (int)request.ListingType,
        request.ListingType == ListingType.Auction ? request.BuyItNowPrice ?? request.StartingBid!.Value : request.Price,
        request.DiscountAmount,
        request.ListingType == ListingType.Auction ? request.StartingBid : null,
        request.ListingType == ListingType.Auction ? request.ReservePrice : null,
        request.ListingType == ListingType.Auction ? request.BuyItNowPrice : null,
        request.ListingType == ListingType.Auction ? request.AuctionStartAt ?? currentListing.AuctionStartAt ?? DateTime.UtcNow : null,
        request.ListingType == ListingType.Auction ? request.AuctionEndAt : null,
        request.Quantity,
        request.FreeShipping,
        request.CategoryId,
        (request.AttributeValues ?? [])
            .Where(v => v.CategoryAttributeId != Guid.Empty && !string.IsNullOrWhiteSpace(v.Value))
            .Select(v => new AttributeEntry(v.CategoryAttributeId, v.Value!.Trim()))
            .ToList(),
        (request.Images ?? [])
            .Where(i => !string.IsNullOrWhiteSpace(i.Url))
            .Select(i => new ImageEntry(i.Url.Trim(), i.AltText?.Trim(), i.SortOrder))
            .ToList()
    );

    private static ListingResponse MapToResponse(Listing l) => new(
        l.Id,
        l.Title,
        l.Description,
        (int)l.ListingType,
        l.Price,
        l.DiscountAmount,
        GetFinalPrice(l),
        l.StartingBid,
        l.ReservePrice,
        l.BuyItNowPrice,
        l.AuctionStartAt,
        l.AuctionEndAt,
        l.Quantity,
        l.FreeShipping,
        (int)l.Status,
        l.IsDeleted,
        l.PrimaryImageUrl,
        l.SellerId,
        $"{l.Seller?.FirstName} {l.Seller?.LastName}".Trim(),
        l.CategoryId,
        l.Category?.Name,
        l.AttributeValues
            .Where(v => !v.IsDeleted)
            .OrderBy(v => v.CategoryAttribute?.SortOrder ?? int.MaxValue)
            .Select(MapAttributeValueToResponse)
            .ToList(),
        l.Images
            .Where(i => !i.IsDeleted)
            .OrderBy(i => i.SortOrder)
            .Select(i => new ListingImageResponse(i.Id, i.Url, i.AltText, i.SortOrder))
            .ToList(),
        l.CreatedAt,
        l.UpdatedAt,
        l.HasPendingVersion
    );

    private static decimal GetFinalPrice(Listing listing) =>
        Math.Max(listing.Price - listing.DiscountAmount, 0);

    private static ListingAttributeValueResponse MapAttributeValueToResponse(ListingAttributeValue v) =>
        new(
            v.CategoryAttributeId,
            v.CategoryAttribute?.Name ?? v.CategoryAttributeId.ToString(),
            v.CategoryAttribute?.DisplayName ?? "Attribute",
            v.CategoryAttribute is null ? (int)AttributeDataType.Text : (int)v.CategoryAttribute.DataType,
            v.Value,
            GetDisplayValue(v));

    private static string? GetDisplayValue(ListingAttributeValue v)
    {
        if (v.CategoryAttribute is null)
            return v.Value;

        if (v.CategoryAttribute.DataType == AttributeDataType.MultiSelect)
        {
            var selected = ParseMultiSelect(v.Value).ToHashSet(StringComparer.OrdinalIgnoreCase);
            return string.Join(", ", v.CategoryAttribute.Options
                .Where(o => selected.Contains(o.Value))
                .OrderBy(o => o.SortOrder)
                .Select(o => o.Label));
        }

        if (v.CategoryAttribute.DataType == AttributeDataType.Dropdown)
            return v.CategoryAttribute.Options.FirstOrDefault(o => o.Value == v.Value)?.Label ?? v.Value;

        return v.Value;
    }
}
