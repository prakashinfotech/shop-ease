using System.Security.Cryptography;
using System.Text;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Infrastructure.Data.Seed;

public static class CategoryFormSeeder
{
    private static readonly DateTime SeededAt = new(2024, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        var forms = BuildForms();
        var categories = await db.Categories.IgnoreQueryFilters().ToListAsync(ct);
        var categoryIdsByKey = new Dictionary<string, Guid>();

        foreach (var form in forms)
        {
            var category = categories.FirstOrDefault(c => c.Id == form.Id)
                ?? categories.FirstOrDefault(c => c.ParentCategoryId == form.ParentId && c.Name == form.Name);

            if (category is null)
            {
                category = new Category
                {
                    Id = form.Id,
                    ParentCategoryId = form.ParentId,
                    Name = form.Name,
                    Description = form.Description,
                    SortOrder = form.SortOrder,
                    CreatedAt = SeededAt,
                    UpdatedAt = SeededAt,
                };

                db.Categories.Add(category);
                categories.Add(category);
            }
            else
            {
                category.ParentCategoryId = form.ParentId;
                category.IsDeleted = false;
                category.DeletedAt = null;
            }

            categoryIdsByKey[form.Key] = category.Id;
        }

        await db.SaveChangesAsync(ct);

        var attributes = await db.CategoryAttributes
            .IgnoreQueryFilters()
            .Include(a => a.Options)
            .ToListAsync(ct);

        foreach (var form in forms)
        {
            var categoryId = categoryIdsByKey[form.Key];

            foreach (var seed in form.Attributes)
            {
                var attribute = attributes.FirstOrDefault(a => a.Id == seed.Id)
                    ?? attributes.FirstOrDefault(a => a.CategoryId == categoryId && a.Name == seed.Name);

                if (attribute is null)
                {
                    attribute = new CategoryAttribute
                    {
                        Id = seed.Id,
                        CategoryId = categoryId,
                        Name = seed.Name,
                        DisplayName = seed.DisplayName,
                        Description = seed.Description,
                        DataType = seed.DataType,
                        IsRequired = seed.IsRequired,
                        IsFilterable = seed.IsFilterable,
                        SortOrder = seed.SortOrder,
                        Placeholder = seed.Placeholder,
                        Unit = seed.Unit,
                        MinLength = seed.MinLength,
                        MaxLength = seed.MaxLength,
                        MinValue = seed.MinValue,
                        MaxValue = seed.MaxValue,
                        RegexPattern = seed.RegexPattern,
                        CreatedAt = SeededAt,
                        UpdatedAt = SeededAt,
                    };

                    db.CategoryAttributes.Add(attribute);
                    attributes.Add(attribute);
                }
                else
                {
                    attribute.IsDeleted = false;
                    attribute.DeletedAt = null;
                }

                foreach (var optionSeed in seed.Options)
                {
                    var option = attribute.Options.FirstOrDefault(o => o.Value == optionSeed.Value);
                    if (option is null)
                    {
                        attribute.Options.Add(new AttributeOption
                        {
                            Id = CreateGuid($"option:{form.Key}:{seed.Name}:{optionSeed.Value}"),
                            Value = optionSeed.Value,
                            Label = optionSeed.Label,
                            SortOrder = optionSeed.SortOrder,
                            IsActive = true,
                            CreatedAt = SeededAt,
                            UpdatedAt = SeededAt,
                        });
                    }
                    else
                    {
                        option.IsActive = true;
                        option.IsDeleted = false;
                        option.DeletedAt = null;
                    }
                }
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private static IReadOnlyList<SeedCategoryForm> BuildForms()
    {
        var electronics = Guid.Parse("10000000-0000-0000-0000-000000000001");
        var fashion = Guid.Parse("10000000-0000-0000-0000-000000000002");
        var home = Guid.Parse("10000000-0000-0000-0000-000000000003");
        var sports = Guid.Parse("10000000-0000-0000-0000-000000000004");
        var vehicles = Guid.Parse("10000000-0000-0000-0000-000000000005");
        var toys = Guid.Parse("10000000-0000-0000-0000-000000000006");
        var books = Guid.Parse("10000000-0000-0000-0000-000000000007");
        var collectibles = Guid.Parse("10000000-0000-0000-0000-000000000008");

        return
        [
            Form(electronics, "smartphones", "Smartphones", "Mobile phones, unlocked phones, and carrier devices.", 1,
                Dropdown("smartphones", "brand", "Brand", 1, true, true, ("apple", "Apple"), ("samsung", "Samsung"), ("google", "Google"), ("oneplus", "OnePlus")),
                Text("smartphones", "model", "Model", 2, "iPhone 15 Pro, Galaxy S24", true, 2, 80),
                Dropdown("smartphones", "storage", "Storage", 3, true, true, ("64gb", "64 GB"), ("128gb", "128 GB"), ("256gb", "256 GB"), ("512gb", "512 GB"), ("1tb", "1 TB")),
                Dropdown("smartphones", "condition", "Condition", 4, true, true, ("new", "New"), ("open_box", "Open Box"), ("used", "Used"), ("for_parts", "For Parts")),
                Dropdown("smartphones", "carrier", "Carrier", 5, false, true, ("unlocked", "Unlocked"), ("att", "AT&T"), ("verizon", "Verizon"), ("tmobile", "T-Mobile"))),

            Form(electronics, "laptops", "Laptops", "Notebook computers, gaming laptops, and ultrabooks.", 2,
                Dropdown("laptops", "brand", "Brand", 1, true, true, ("apple", "Apple"), ("dell", "Dell"), ("hp", "HP"), ("lenovo", "Lenovo"), ("asus", "ASUS")),
                Text("laptops", "processor", "Processor", 2, "Intel i7, Ryzen 7, M3", true, 2, 80),
                Dropdown("laptops", "ram", "RAM", 3, true, true, ("8gb", "8 GB"), ("16gb", "16 GB"), ("32gb", "32 GB"), ("64gb", "64 GB")),
                Text("laptops", "storage", "Storage", 4, "512GB SSD", true, 2, 80),
                Decimal("laptops", "screen_size", "Screen Size", 5, "inches", false, 8, 20)),

            Form(fashion, "mens_clothing", "Men's Clothing", "Shirts, jeans, jackets, and everyday menswear.", 1,
                Text("mens_clothing", "brand", "Brand", 1, "Nike, Levi's, Zara", true, 2, 80),
                Dropdown("mens_clothing", "size", "Size", 2, true, true, ("xs", "XS"), ("s", "S"), ("m", "M"), ("l", "L"), ("xl", "XL"), ("xxl", "XXL")),
                Text("mens_clothing", "color", "Color", 3, "Navy blue", true, 2, 50),
                Text("mens_clothing", "material", "Material", 4, "Cotton, denim, wool", false, 2, 80),
                Dropdown("mens_clothing", "condition", "Condition", 5, true, true, ("new_tags", "New with Tags"), ("new_no_tags", "New without Tags"), ("preowned", "Pre-owned"))),

            Form(fashion, "womens_clothing", "Women's Clothing", "Dresses, tops, jeans, outerwear, and more.", 2,
                Text("womens_clothing", "brand", "Brand", 1, "H&M, Zara, Free People", true, 2, 80),
                Dropdown("womens_clothing", "size", "Size", 2, true, true, ("xs", "XS"), ("s", "S"), ("m", "M"), ("l", "L"), ("xl", "XL"), ("plus", "Plus")),
                Text("womens_clothing", "color", "Color", 3, "Black", true, 2, 50),
                Dropdown("womens_clothing", "style", "Style", 4, false, true, ("casual", "Casual"), ("formal", "Formal"), ("activewear", "Activewear"), ("vintage", "Vintage")),
                Dropdown("womens_clothing", "condition", "Condition", 5, true, true, ("new_tags", "New with Tags"), ("new_no_tags", "New without Tags"), ("preowned", "Pre-owned"))),

            Form(home, "furniture", "Furniture", "Indoor furniture for living rooms, bedrooms, and offices.", 1,
                Dropdown("furniture", "room", "Room", 1, true, true, ("living_room", "Living Room"), ("bedroom", "Bedroom"), ("office", "Office"), ("dining", "Dining")),
                Dropdown("furniture", "material", "Material", 2, true, true, ("wood", "Wood"), ("metal", "Metal"), ("fabric", "Fabric"), ("leather", "Leather")),
                Text("furniture", "dimensions", "Dimensions", 3, "72 x 36 x 30 in", true, 3, 120),
                Text("furniture", "color", "Color", 4, "Walnut brown", false, 2, 60),
                Dropdown("furniture", "condition", "Condition", 5, true, true, ("new", "New"), ("used", "Used"), ("refurbished", "Refurbished"))),

            Form(home, "kitchen_appliances", "Kitchen Appliances", "Small and large appliances for kitchens.", 2,
                Text("kitchen_appliances", "brand", "Brand", 1, "KitchenAid, Ninja, LG", true, 2, 80),
                Dropdown("kitchen_appliances", "appliance_type", "Appliance Type", 2, true, true, ("blender", "Blender"), ("mixer", "Mixer"), ("microwave", "Microwave"), ("coffee_maker", "Coffee Maker")),
                Number("kitchen_appliances", "wattage", "Power", 3, "watts", false, 50, 5000),
                Text("kitchen_appliances", "capacity", "Capacity", 4, "1.5 L, 6 qt", false, 1, 50),
                Dropdown("kitchen_appliances", "condition", "Condition", 5, true, true, ("new", "New"), ("used", "Used"), ("open_box", "Open Box"))),

            Form(sports, "fitness_equipment", "Fitness Equipment", "Exercise machines, weights, benches, and training gear.", 1,
                Dropdown("fitness_equipment", "equipment_type", "Equipment Type", 1, true, true, ("treadmill", "Treadmill"), ("weights", "Weights"), ("bike", "Exercise Bike"), ("bench", "Bench")),
                Text("fitness_equipment", "brand", "Brand", 2, "Bowflex, NordicTrack", false, 2, 80),
                Number("fitness_equipment", "weight_capacity", "Weight Capacity", 3, "lb", false, 1, 1000),
                Text("fitness_equipment", "dimensions", "Dimensions", 4, "48 x 24 x 60 in", false, 3, 120),
                Dropdown("fitness_equipment", "condition", "Condition", 5, true, true, ("new", "New"), ("used", "Used"), ("refurbished", "Refurbished"))),

            Form(sports, "outdoor_recreation", "Outdoor Recreation", "Camping, hiking, fishing, and outdoor adventure gear.", 2,
                Dropdown("outdoor_recreation", "activity", "Activity", 1, true, true, ("camping", "Camping"), ("hiking", "Hiking"), ("fishing", "Fishing"), ("cycling", "Cycling")),
                Text("outdoor_recreation", "brand", "Brand", 2, "Coleman, The North Face", false, 2, 80),
                Text("outdoor_recreation", "size", "Size", 3, "2-person, medium", false, 1, 60),
                Text("outdoor_recreation", "material", "Material", 4, "Nylon, aluminum", false, 2, 80),
                Dropdown("outdoor_recreation", "condition", "Condition", 5, true, true, ("new", "New"), ("used", "Used"), ("open_box", "Open Box"))),

            Form(vehicles, "cars", "Cars", "Passenger cars, sedans, SUVs, and trucks.", 1,
                Text("cars", "make", "Make", 1, "Toyota", true, 2, 60),
                Text("cars", "model", "Model", 2, "Camry", true, 1, 80),
                Number("cars", "year", "Year", 3, null, true, 1900, 2035),
                Number("cars", "mileage", "Mileage", 4, "miles", true, 0, 1000000),
                Dropdown("cars", "transmission", "Transmission", 5, true, true, ("automatic", "Automatic"), ("manual", "Manual"))),

            Form(vehicles, "motorcycles", "Motorcycles", "Street bikes, cruisers, scooters, and off-road motorcycles.", 2,
                Text("motorcycles", "make", "Make", 1, "Honda", true, 2, 60),
                Text("motorcycles", "model", "Model", 2, "CBR500R", true, 1, 80),
                Number("motorcycles", "year", "Year", 3, null, true, 1900, 2035),
                Number("motorcycles", "mileage", "Mileage", 4, "miles", true, 0, 500000),
                Dropdown("motorcycles", "engine_size", "Engine Size", 5, false, true, ("under_250", "Under 250cc"), ("250_500", "250-500cc"), ("500_1000", "500-1000cc"), ("1000_plus", "1000cc+"))),

            Form(toys, "action_figures", "Action Figures", "Collectible figures, playsets, and character toys.", 1,
                Text("action_figures", "character", "Character", 1, "Spider-Man", true, 1, 80),
                Text("action_figures", "franchise", "Franchise", 2, "Marvel, Star Wars", true, 2, 80),
                Dropdown("action_figures", "scale", "Scale", 3, false, true, ("three_quarter", "3.75 in"), ("six", "6 in"), ("twelve", "12 in"), ("other", "Other")),
                Dropdown("action_figures", "packaging", "Packaging", 4, true, true, ("sealed", "Sealed"), ("opened", "Opened"), ("loose", "Loose")),
                Dropdown("action_figures", "condition", "Condition", 5, true, true, ("new", "New"), ("used", "Used"), ("damaged", "Damaged"))),

            Form(toys, "board_games", "Board Games", "Tabletop games, card games, and family games.", 2,
                Text("board_games", "publisher", "Publisher", 1, "Hasbro, Ravensburger", false, 2, 80),
                Dropdown("board_games", "player_count", "Player Count", 2, true, true, ("1", "1 Player"), ("2", "2 Players"), ("3_4", "3-4 Players"), ("5_plus", "5+ Players")),
                Dropdown("board_games", "age_range", "Age Range", 3, false, true, ("3_plus", "3+"), ("8_plus", "8+"), ("12_plus", "12+"), ("18_plus", "18+")),
                Dropdown("board_games", "condition", "Condition", 4, true, true, ("new", "New"), ("used_complete", "Used Complete"), ("missing_pieces", "Missing Pieces")),
                Boolean("board_games", "complete", "Complete Set", 5, false)),

            Form(books, "fiction_books", "Fiction Books", "Novels, short stories, classics, and genre fiction.", 1,
                Text("fiction_books", "author", "Author", 1, "Author name", true, 2, 100),
                Dropdown("fiction_books", "genre", "Genre", 2, true, true, ("fantasy", "Fantasy"), ("mystery", "Mystery"), ("romance", "Romance"), ("sci_fi", "Science Fiction"), ("literary", "Literary")),
                Dropdown("fiction_books", "format", "Format", 3, true, true, ("hardcover", "Hardcover"), ("paperback", "Paperback"), ("ebook", "E-book"), ("audiobook", "Audiobook")),
                Text("fiction_books", "language", "Language", 4, "English", false, 2, 50),
                Dropdown("fiction_books", "condition", "Condition", 5, true, true, ("new", "New"), ("like_new", "Like New"), ("good", "Good"), ("acceptable", "Acceptable"))),

            Form(books, "textbooks", "Academic Textbooks", "School, college, technical, and professional textbooks.", 2,
                Dropdown("textbooks", "subject", "Subject", 1, true, true, ("math", "Mathematics"), ("science", "Science"), ("business", "Business"), ("engineering", "Engineering"), ("medicine", "Medicine")),
                Text("textbooks", "author", "Author", 2, "Author name", true, 2, 100),
                Text("textbooks", "edition", "Edition", 3, "5th Edition", false, 1, 50),
                Text("textbooks", "isbn", "ISBN", 4, "978-0-123456-47-2", false, 10, 20),
                Dropdown("textbooks", "condition", "Condition", 5, true, true, ("new", "New"), ("like_new", "Like New"), ("good", "Good"), ("acceptable", "Acceptable"))),

            Form(collectibles, "trading_cards", "Trading Cards", "Sports cards, TCG cards, sealed packs, and graded cards.", 1,
                Dropdown("trading_cards", "card_type", "Card Type", 1, true, true, ("sports", "Sports"), ("pokemon", "Pokemon"), ("magic", "Magic"), ("yugioh", "Yu-Gi-Oh!")),
                Text("trading_cards", "card_name", "Card Name", 2, "Card/player name", true, 1, 120),
                Number("trading_cards", "year", "Year", 3, null, false, 1800, 2035),
                Dropdown("trading_cards", "grade", "Grade", 4, false, true, ("ungraded", "Ungraded"), ("psa", "PSA"), ("bgs", "BGS"), ("cgc", "CGC")),
                Dropdown("trading_cards", "rarity", "Rarity", 5, false, true, ("common", "Common"), ("rare", "Rare"), ("ultra_rare", "Ultra Rare"), ("limited", "Limited"))),

            Form(collectibles, "coins", "Coins", "Collectible coins, bullion coins, and currency sets.", 2,
                Text("coins", "country", "Country", 1, "United States", true, 2, 80),
                Number("coins", "year", "Year", 2, null, false, 1, 2035),
                Text("coins", "denomination", "Denomination", 3, "Quarter, 1 oz", true, 1, 80),
                Dropdown("coins", "metal", "Metal", 4, false, true, ("gold", "Gold"), ("silver", "Silver"), ("copper", "Copper"), ("nickel", "Nickel"), ("mixed", "Mixed")),
                Dropdown("coins", "grade", "Grade", 5, false, true, ("ungraded", "Ungraded"), ("circulated", "Circulated"), ("uncirculated", "Uncirculated"), ("proof", "Proof")))
        ];
    }

    private static SeedCategoryForm Form(Guid parentId, string key, string name, string description, int sortOrder, params SeedAttribute[] attributes) =>
        new(CreateGuid($"category:{key}"), parentId, key, name, description, sortOrder, attributes);

    private static SeedAttribute Text(
        string categoryKey,
        string name,
        string label,
        int sortOrder,
        string? placeholder,
        bool required,
        int? minLength,
        int? maxLength) =>
        new(CreateGuid($"attribute:{categoryKey}:{name}"), name, label, AttributeDataType.Text, required, true, sortOrder, null, placeholder, null, minLength, maxLength, null, null, null, []);

    private static SeedAttribute Number(
        string categoryKey,
        string name,
        string label,
        int sortOrder,
        string? unit,
        bool required,
        decimal? min,
        decimal? max) =>
        new(CreateGuid($"attribute:{categoryKey}:{name}"), name, label, AttributeDataType.Number, required, true, sortOrder, null, null, unit, null, null, min, max, null, []);

    private static SeedAttribute Decimal(
        string categoryKey,
        string name,
        string label,
        int sortOrder,
        string? unit,
        bool required,
        decimal? min,
        decimal? max) =>
        new(CreateGuid($"attribute:{categoryKey}:{name}"), name, label, AttributeDataType.Decimal, required, true, sortOrder, null, null, unit, null, null, min, max, null, []);

    private static SeedAttribute Boolean(string categoryKey, string name, string label, int sortOrder, bool required) =>
        new(CreateGuid($"attribute:{categoryKey}:{name}"), name, label, AttributeDataType.Boolean, required, true, sortOrder, null, null, null, null, null, null, null, null, []);

    private static SeedAttribute Dropdown(
        string categoryKey,
        string name,
        string label,
        int sortOrder,
        bool required,
        bool filterable,
        params (string Value, string Label)[] options) =>
        new(
            CreateGuid($"attribute:{categoryKey}:{name}"),
            name,
            label,
            AttributeDataType.Dropdown,
            required,
            filterable,
            sortOrder,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            null,
            options.Select((o, index) => new SeedOption(o.Value, o.Label, index)).ToList());

    private static Guid CreateGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }

    private sealed record SeedCategoryForm(
        Guid Id,
        Guid ParentId,
        string Key,
        string Name,
        string Description,
        int SortOrder,
        IReadOnlyList<SeedAttribute> Attributes);

    private sealed record SeedAttribute(
        Guid Id,
        string Name,
        string DisplayName,
        AttributeDataType DataType,
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
        IReadOnlyList<SeedOption> Options);

    private sealed record SeedOption(string Value, string Label, int SortOrder);
}
