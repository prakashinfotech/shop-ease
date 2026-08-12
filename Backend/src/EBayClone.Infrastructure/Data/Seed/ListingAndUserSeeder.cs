using System.Security.Cryptography;
using System.Text;
using EBayClone.Domain.Entities;
using EBayClone.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace EBayClone.Infrastructure.Data.Seed;

public static class ListingAndUserSeeder
{
    private static readonly DateTime SeededAt = new(2024, 6, 1, 0, 0, 0, DateTimeKind.Utc);

    private static readonly Guid[] UserIds =
    [
        Guid.Parse("20000000-0000-0000-0000-000000000001"),
        Guid.Parse("20000000-0000-0000-0000-000000000002"),
        Guid.Parse("20000000-0000-0000-0000-000000000003"),
        Guid.Parse("20000000-0000-0000-0000-000000000004"),
        Guid.Parse("20000000-0000-0000-0000-000000000005"),
        Guid.Parse("20000000-0000-0000-0000-000000000006"),
        Guid.Parse("20000000-0000-0000-0000-000000000007"),
        Guid.Parse("20000000-0000-0000-0000-000000000008"),
        Guid.Parse("20000000-0000-0000-0000-000000000009"),
        Guid.Parse("20000000-0000-0000-0000-000000000010"),
    ];

    private static readonly (string First, string Last, string Email)[] UserInfo =
    [
        ("Alice",  "Smith",     "alice.smith@example.com"),
        ("Bob",    "Johnson",   "bob.johnson@example.com"),
        ("Carol",  "Williams",  "carol.williams@example.com"),
        ("David",  "Brown",     "david.brown@example.com"),
        ("Eve",    "Jones",     "eve.jones@example.com"),
        ("Frank",  "Miller",    "frank.miller@example.com"),
        ("Grace",  "Davis",     "grace.davis@example.com"),
        ("Henry",  "Garcia",    "henry.garcia@example.com"),
        ("Iris",   "Rodriguez", "iris.rodriguez@example.com"),
        ("Jack",   "Wilson",    "jack.wilson@example.com"),
    ];

    public static async Task SeedAsync(AppDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.IgnoreQueryFilters().AnyAsync(u => u.Id == UserIds[0], ct))
            return;

        var hash = BCrypt.Net.BCrypt.HashPassword("Password123!");

        for (var i = 0; i < UserIds.Length; i++)
        {
            var (first, last, email) = UserInfo[i];
            db.Users.Add(new User
            {
                Id = UserIds[i],
                FirstName = first,
                LastName = last,
                Email = email,
                PasswordHash = hash,
                AccountType = AccountType.Personal,
                Role = UserRole.User,
                IsEmailVerified = true,
                CreatedAt = SeededAt.AddDays(i),
                UpdatedAt = SeededAt.AddDays(i),
            });
        }

        await db.SaveChangesAsync(ct);

        var subcategories = await db.Categories
            .IgnoreQueryFilters()
            .Where(c => c.ParentCategoryId != null)
            .ToDictionaryAsync(c => c.Name, ct);

        var listingIndex = 0;
        foreach (var (subName, items) in GetAllListings())
        {
            if (!subcategories.TryGetValue(subName, out var sub))
                continue;

            for (var i = 0; i < items.Length; i++)
            {
                var item = items[i];
                db.Listings.Add(new Listing
                {
                    Id = CreateGuid($"listing:{subName}:{i}"),
                    Title = item.Title,
                    Description = item.Description,
                    Price = item.Price,
                    Quantity = 1,
                    FreeShipping = item.FreeShipping,
                    ListingType = ListingType.FixedPrice,
                    Status = ListingStatus.Active,
                    SellerId = UserIds[listingIndex % UserIds.Length],
                    CategoryId = sub.Id,
                    CreatedAt = SeededAt.AddDays(listingIndex % 60),
                    UpdatedAt = SeededAt.AddDays(listingIndex % 60),
                });
                listingIndex++;
            }
        }

        await db.SaveChangesAsync(ct);
    }

    private sealed record LD(string Title, string Description, decimal Price, bool FreeShipping);

    private static IEnumerable<(string SubName, LD[] Items)> GetAllListings() =>
    [
        ("Smartphones",        Smartphones),
        ("Laptops",            Laptops),
        ("Men's Clothing",     MensClothing),
        ("Women's Clothing",   WomensClothing),
        ("Furniture",          Furniture),
        ("Kitchen Appliances", KitchenAppliances),
        ("Fitness Equipment",  FitnessEquipment),
        ("Outdoor Recreation", OutdoorRecreation),
        ("Cars",               Cars),
        ("Motorcycles",        Motorcycles),
        ("Action Figures",     ActionFigures),
        ("Board Games",        BoardGames),
        ("Fiction Books",      FictionBooks),
        ("Academic Textbooks", Textbooks),
        ("Trading Cards",      TradingCards),
        ("Coins",              Coins),
    ];

    // ── Smartphones ───────────────────────────────────────────────────────────

    private static readonly LD[] Smartphones =
    [
        new("Apple iPhone 15 Pro 256GB Unlocked Natural Titanium",   "Like-new iPhone 15 Pro with A17 Pro chip and titanium design. Unlocked for all carriers.", 989.99m, true),
        new("Samsung Galaxy S24 Ultra 512GB Titanium Black",         "Samsung flagship with built-in S Pen and 200MP camera. Excellent condition, minimal use.", 1179.99m, true),
        new("Google Pixel 8 Pro 128GB Obsidian Unlocked",            "AI-powered flagship with Tensor G3 chip and 7 years of guaranteed OS updates.", 799.99m, true),
        new("Apple iPhone 14 128GB Blue Unlocked",                   "iPhone 14 in great condition. Works on all major carriers. Original box included.", 649.99m, false),
        new("Samsung Galaxy A54 5G 128GB Awesome Violet",            "Mid-range 5G with 50MP camera and 5000 mAh battery. Brand new in sealed box.", 349.99m, false),
        new("OnePlus 12 256GB Silky Black Unlocked",                 "OnePlus 12 with Snapdragon 8 Gen 3 and 80W SUPERVOOC charging. Like new.", 699.99m, true),
        new("Apple iPhone 15 256GB Pink Open Box",                   "iPhone 15 with Dynamic Island and USB-C. Opened but unused, all accessories included.", 849.99m, true),
        new("Samsung Galaxy S23 256GB Phantom Green",                "Galaxy S23 in mint condition. Snapdragon 8 Gen 2 and 50MP triple camera system.", 649.99m, false),
        new("Google Pixel 7a 128GB Snow Unlocked",                   "Pixel 7a with Tensor G2, 64MP camera, and wireless charging. Excellent condition.", 449.99m, true),
        new("Apple iPhone 13 128GB Midnight Unlocked",               "iPhone 13 with A15 Bionic chip. Light use over 18 months, great condition.", 529.99m, false),
        new("Samsung Galaxy Z Fold 5 256GB Phantom Black",           "Foldable flagship with 7.6-inch main display and S Pen support. Nearly new.", 1299.99m, true),
        new("OnePlus Nord CE3 Lite 5G 128GB Pastel Lime",            "Budget 5G smartphone with 108MP camera and 67W charging. Brand new sealed box.", 299.99m, false),
        new("Apple iPhone 15 Plus 256GB Blue Unlocked",              "iPhone 15 Plus with 6.7-inch display, A16 Bionic, and all-day battery. Unlocked.", 899.99m, true),
        new("Samsung Galaxy S24 128GB Onyx Black",                   "Galaxy S24 with Galaxy AI features, 50MP camera, and 7 years of OS updates.", 799.99m, true),
        new("Google Pixel 8 128GB Hazel Unlocked",                   "Pixel 8 with Tensor G3, Best Take, Photo Unblur, and 7 years of guaranteed updates.", 669.99m, true),
    ];

    // ── Laptops ───────────────────────────────────────────────────────────────

    private static readonly LD[] Laptops =
    [
        new("Apple MacBook Pro 14 M3 Pro 512GB Space Black",   "MacBook Pro 14-inch with M3 Pro chip and 18 GB RAM. Perfect for pro workflows.", 1799.99m, true),
        new("Dell XPS 15 Intel i7-13700H 16GB 512GB",          "Dell XPS 15 with OLED display and 13th-Gen Core i7. Light use, pristine condition.", 1299.99m, true),
        new("HP Spectre x360 13.5 Intel i5 2-in-1",            "Convertible HP Spectre with OLED touch display and built-in stylus. Like new.", 1099.99m, true),
        new("Lenovo ThinkPad X1 Carbon Gen 11 14 i7",          "Business ultrabook with MIL-SPEC durability. 16 GB RAM, 512 GB SSD. Excellent.", 1199.99m, false),
        new("ASUS ROG Zephyrus G14 2023 RTX 4060 14",          "Gaming laptop with AMD Ryzen 9 and NVIDIA RTX 4060. Rarely used.", 1249.99m, true),
        new("Apple MacBook Air 15 M2 256GB Midnight",           "MacBook Air 15-inch with M2 chip and 8 GB unified memory. Great battery life.", 1149.99m, true),
        new("Dell Inspiron 15 i5-1235U 8GB 256GB",             "Everyday laptop for school and work. Good condition, minor wear on palm rest.", 599.99m, false),
        new("HP Pavilion 15 Ryzen 5 16GB 512GB SSD",           "Reliable HP Pavilion with AMD Ryzen 5 and Full HD display. Like new.", 649.99m, false),
        new("Lenovo IdeaPad 5 Pro 14 AMD Ryzen 7 16GB",        "Premium IdeaPad with 2K display and Ryzen 7. Purchased 4 months ago.", 749.99m, true),
        new("ASUS VivoBook 15 OLED Intel i7 16GB 512GB",       "VivoBook with stunning OLED display and Core i7-12700H. Barely used.", 699.99m, true),
        new("Apple MacBook Pro 16 M2 Max 1TB Silver",           "Top-spec MacBook Pro 16 with M2 Max and 32 GB RAM. Professional powerhouse.", 2499.99m, true),
        new("Dell G15 Gaming Intel i7 RTX 3060 15.6",          "Dell G15 gaming laptop with RTX 3060 and 165 Hz display. Good gaming condition.", 899.99m, false),
        new("HP Envy x360 15 Ryzen 7 512GB Convertible",       "HP Envy x360 with AMD Ryzen 7 and OLED touch screen. Excellent condition.", 849.99m, true),
        new("Lenovo Legion 5 Pro Ryzen 7 RTX 4070 16",         "Gaming powerhouse with RTX 4070 and Ryzen 7 7745HX. QHD 165 Hz display.", 1399.99m, false),
        new("ASUS ZenBook 14 OLED Intel i7-1260P 16GB",        "Ultra-slim ZenBook with OLED display and Intel Evo certification. Excellent.", 999.99m, true),
    ];

    // ── Men's Clothing ────────────────────────────────────────────────────────

    private static readonly LD[] MensClothing =
    [
        new("Nike Tech Fleece Jogger Set XL Black",             "Matching Nike Tech Fleece hoodie and pants. Worn twice, excellent condition.", 129.99m, true),
        new("Levi's 501 Original Jeans 34x32 Dark Wash",       "Classic Levi's 501 straight fit. Dark indigo wash, lightly worn.", 59.99m, false),
        new("Ralph Lauren Polo Shirt M Classic Navy",           "Authentic Ralph Lauren Polo shirt, size medium. New with tags attached.", 45.99m, true),
        new("Patagonia Better Sweater Fleece Jacket L",         "Lightweight Patagonia fleece with snap collar. Worn a handful of times.", 89.99m, true),
        new("Carhartt Work Jacket XL Brown New with Tags",      "Carhartt chore coat in brown duck canvas. Brand new with original tags.", 79.99m, false),
        new("Tommy Hilfiger Slim Fit Dress Shirt XL White",    "Crisp Tommy Hilfiger dress shirt for work or formal occasions. Like new.", 39.99m, true),
        new("Under Armour Hooded Sweatshirt 2XL Steel Gray",   "UA Rival Fleece hoodie with moisture-wicking fabric. Very comfortable.", 49.99m, false),
        new("Zara Slim Fit Chinos 32x30 Khaki",                "Zara slim-fit chinos in light khaki. Worn a few times, great condition.", 29.99m, true),
        new("Columbia Fleece Vest L Navy Blue",                 "Columbia Steens Mountain Vest. Perfect mid-layer, light use only.", 54.99m, false),
        new("Nike Dri-FIT Tapered Jogger Pants L Black",       "Nike Dri-FIT training pants with tapered fit. Excellent workout condition.", 44.99m, true),
        new("Levi's Trucker Jacket M Dark Wash Denim",         "Classic Levi's denim trucker jacket. Broken in nicely, great condition.", 59.99m, false),
        new("J.Crew Slim Wool Blazer 40R Charcoal",            "J.Crew Ludlow blazer in Italian wool. Perfect for office or events.", 89.99m, true),
        new("Adidas Tiro Track Pants XL Black White",          "Adidas Tiro training pants with side zip pockets. Like new condition.", 44.99m, true),
        new("Brooks Brothers Oxford Shirt L Blue Stripe",      "Classic Brooks Brothers cotton Oxford with button-down collar. Excellent.", 49.99m, false),
        new("The North Face Fleece Hoodie M Heather Gray",     "TNF 100 Glacier full-zip hoodie. Worn a few times, great warmth.", 64.99m, true),
    ];

    // ── Women's Clothing ──────────────────────────────────────────────────────

    private static readonly LD[] WomensClothing =
    [
        new("Free People Floral Maxi Dress M Boho Blue",       "Free People long floral dress with flutter sleeves. Worn once.", 59.99m, true),
        new("Zara High-Waist Straight Jeans 28 Black",         "Zara straight-leg jeans with high waist and raw hem. Like new.", 34.99m, false),
        new("Anthropologie Linen Blend Blouse S Ivory",        "Lightweight linen blend top with pintuck details. New with tags.", 44.99m, true),
        new("Lululemon Align Leggings 25 in Size 6 Black",     "Classic Lululemon Align in buttery-soft Nulu fabric. Worn twice.", 79.99m, true),
        new("H&M Oversized Blazer US 8 Camel",                 "Trendy oversized blazer in camel. Pairs with jeans or dresses.", 39.99m, false),
        new("ASOS Floral Mini Dress US 10 Multicolor",         "Fun floral print mini dress with puff sleeves. Like new condition.", 29.99m, true),
        new("Madewell Perfect Vintage Jeans 27 Medium",        "Madewell vintage-inspired straight jeans. Broken in just right.", 54.99m, false),
        new("Nike Women Dri-FIT Training T-Shirt M Pink",      "Nike training tee with moisture-wicking Dri-FIT technology. Excellent.", 24.99m, true),
        new("Reformation Slip Dress S Forest Green",           "Reformation linen slip dress in forest green. Worn once to an event.", 79.99m, true),
        new("Ann Taylor Ponte Knit Blazer 6P Navy",            "Tailored ponte knit blazer from Ann Taylor Petites. Like new.", 49.99m, false),
        new("J.Crew Breton Stripe Long Sleeve Tee L Red",     "Classic J.Crew Breton stripe tee in soft cotton. Light use.", 19.99m, true),
        new("Banana Republic 100% Silk Blouse 8 Ivory",       "Banana Republic silk blouse. Dry cleaned, excellent condition.", 54.99m, false),
        new("Everlane 90s Cheeky Straight Jeans 29 Light",    "Everlane high-rise cheeky straight jeans. Light vintage wash.", 44.99m, true),
        new("Gap Fit High Rise 7-8 Length Leggings M Black",  "GapFit breathe leggings with high waist. Great for yoga or gym.", 29.99m, true),
        new("Vince 100% Cashmere Crewneck Sweater XS Camel",  "Luxurious Vince cashmere sweater. Dry cleaned, like new.", 89.99m, true),
    ];

    // ── Furniture ─────────────────────────────────────────────────────────────

    private static readonly LD[] Furniture =
    [
        new("IKEA MALM 6-Drawer Dresser White 31x55 in",       "MALM dresser in white with smooth-gliding drawers. Good condition.", 149.99m, false),
        new("Mid-Century Modern Coffee Table Walnut Finish",   "Solid wood mid-century coffee table with tapered legs. Excellent.", 199.99m, false),
        new("West Elm Slope Accent Chair Gray Velvet",         "Plush velvet accent chair from West Elm. Light use, great shape.", 349.99m, false),
        new("Ashley Furniture Sectional Sofa L-Shape Charcoal","Ashley L-shape sectional with chaise. Good condition, no pets.", 799.99m, false),
        new("IKEA KALLAX 4x4 Grid Shelf Unit White",           "KALLAX shelf with 16 open cubbies. Perfect for books or baskets.", 149.99m, false),
        new("Twin Loft Bed with Desk and Shelving White",      "Full loft bed with integrated desk and storage. Great for kids.", 499.99m, false),
        new("Modern 88 Inch Sofa Charme Tan Leather Look",     "Article Sven-style sofa in charme tan. Lightly used, great shape.", 1199.99m, false),
        new("IKEA ALEX Desk with Drawers White 47x23 in",      "IKEA ALEX writing desk with deep drawers. Clean and scratch-free.", 199.99m, false),
        new("High-Back Leather Executive Office Chair Black",  "Adjustable leather office chair with lumbar support. Very comfortable.", 349.99m, false),
        new("Solid Walnut Dining Table 60x36 Modern Style",    "Solid walnut dining table for 6. Minor surface marks, very sturdy.", 449.99m, false),
        new("IKEA BRIMNES Queen Bed Frame with Headboard",     "IKEA BRIMNES queen bed with headboard storage compartment. Good.", 249.99m, false),
        new("King Platform Bed Button-Tufted Headboard Gray",  "King platform bed with tufted velvet headboard. Barely used.", 699.99m, false),
        new("Electric L-Shaped Standing Desk 63 in Black",    "Dual-motor height-adjustable L-desk with memory presets. Excellent.", 299.99m, false),
        new("5-Shelf Bookcase Scandinavian Oak Veneer Finish", "Clean-lined bookcase in oak veneer. Light use, no major marks.", 179.99m, false),
        new("6-Piece Outdoor Teak Dining Set Patio Table",     "Teak dining table, 4 chairs, 1 bench. Naturally weathered finish.", 899.99m, false),
    ];

    // ── Kitchen Appliances ────────────────────────────────────────────────────

    private static readonly LD[] KitchenAppliances =
    [
        new("KitchenAid 5-Qt Artisan Stand Mixer Empire Red",  "Iconic KitchenAid stand mixer with full accessory kit. Excellent.", 349.99m, true),
        new("Ninja Professional Plus Blender 1400W BL641",     "Ninja blender with Auto-iQ and 72 oz pitcher. Used lightly.", 99.99m, true),
        new("Breville Smart Oven Pro 1800W Stainless BOV845",  "12-function convection oven with Element IQ. Rarely used.", 249.99m, false),
        new("Instant Pot Duo 7-in-1 8-Quart Pressure Cooker", "Large Instant Pot with 7 cooking functions. Used several times.", 89.99m, true),
        new("Vitamix 5200 Professional Blender Black",         "Professional Vitamix with variable speed control. Excellent.", 349.99m, false),
        new("De'Longhi Dedica 15-Bar Espresso Machine EC680",  "Slim espresso maker with 15-bar pump and milk frother. Like new.", 199.99m, true),
        new("Cuisinart 12-Cup Programmable Coffee Maker",      "DCC-3200 with brew-strength control and 1-4 cup setting.", 69.99m, true),
        new("Keurig K-Elite Single-Serve Coffee Maker Silver", "K-Elite with iced coffee mode and strong brew option. Good.", 109.99m, false),
        new("Ninja AF101 Air Fryer 4-Quart Black",             "Ninja air fryer with wide temperature range. Used under 10 times.", 79.99m, true),
        new("KitchenAid 9-Speed Digital Hand Mixer White",     "Lightweight hand mixer with soft-start feature. Good condition.", 59.99m, true),
        new("Breville Barista Express Espresso Machine BES870", "All-in-one espresso maker with built-in grinder. Lightly used.", 499.99m, false),
        new("Vitamix Explorian E310 48oz Certified Recond",    "Certified reconditioned Vitamix with full warranty. Like new.", 249.99m, true),
        new("Instant Pot Duo Crisp 8Qt Air Fryer Combo",       "Instant Pot with integrated air fryer lid. Used a handful of times.", 129.99m, true),
        new("Cuisinart DFP-14BCWNY 14-Cup Food Processor",    "14-cup food processor with multiple blades and discs. Very good.", 149.99m, false),
        new("Philips 3000 Series XXL Air Fryer 3lb Black",    "XXL air fryer with fat reduction up to 90%. Barely used.", 139.99m, true),
    ];

    // ── Fitness Equipment ─────────────────────────────────────────────────────

    private static readonly LD[] FitnessEquipment =
    [
        new("Bowflex SelectTech 552 Adjustable Dumbbells Pair","Pair of SelectTech 552 with stand. Replaces 15 sets of weights.", 299.99m, false),
        new("NordicTrack Commercial 1750 Treadmill iFit",      "Feature-packed treadmill with 14-inch touchscreen and incline.", 1499.99m, false),
        new("Peloton Bike Plus Stationary Indoor Cycle",       "Peloton Bike+ with 23-inch rotating touchscreen. 1 year old.", 1799.99m, false),
        new("CAP Barbell Rubber Olympic 110lb Weight Set",     "Olympic barbell with bumper plates. Great for home gym setup.", 199.99m, false),
        new("TRX All-in-One Suspension Training System",       "Full TRX kit with door anchor, mesh bag, and workout guide.", 149.99m, true),
        new("Sunny Health SF-RB4616 Recumbent Exercise Bike",  "Recumbent bike with 24 resistance levels and LCD monitor.", 249.99m, false),
        new("Life Fitness Club Series Elliptical Cross-Trainer","Commercial-grade elliptical. Smooth, quiet, and very durable.", 1999.99m, false),
        new("Bowflex PR3000 Home Gym 50+ Exercises",           "Bowflex cable home gym with lat tower and leg extension.", 899.99m, false),
        new("Concept2 RowErg Model D Rowing Machine PM5",      "Industry-standard rowing machine with PM5 monitor. Excellent.", 999.99m, false),
        new("PowerBlock Elite 50 Adjustable Dumbbells Pair",   "Compact adjustable dumbbells from 10-50 lbs per hand. Excellent.", 349.99m, false),
        new("XMark Adjustable FID Utility Weight Bench",       "Heavy-duty bench with 7 back pad and 3 seat positions.", 179.99m, false),
        new("Cap Barbell 40lb Adjustable Dumbbell Set",        "Threaded dumbbell handles with weight plates. Good beginner set.", 59.99m, false),
        new("Marcy Multi-Functional Home Gym 150lb Stack",     "Complete cable machine with lat pulldown and low pulley.", 399.99m, false),
        new("Rogue Monster Rack 3x3 Steel Power Cage",         "Heavy-duty power rack for squats and bench. Industrial quality.", 799.99m, false),
        new("LifeFitness T3 Treadmill with FlexDeck",          "FlexDeck cushioned belt with 12 built-in programs. Used.", 349.99m, false),
    ];

    // ── Outdoor Recreation ────────────────────────────────────────────────────

    private static readonly LD[] OutdoorRecreation =
    [
        new("Coleman Sundome 4-Person Camping Tent",           "Weather-resistant dome tent with easy clip setup. Used twice.", 79.99m, true),
        new("REI Co-op Quarter Dome SL2 Backpacking Tent",    "Ultralight 2-person tent at just 2.1 lbs. Excellent condition.", 249.99m, true),
        new("Marmot Hydrogen 30 Down Sleeping Bag Regular",    "Ultralight 30-degree down sleeping bag. Packed tight, barely used.", 179.99m, true),
        new("Osprey Atmos AG 65 Backpack Men Large",           "Anti-gravity suspended mesh backpack for torso 18-20 in.", 249.99m, false),
        new("Shimano Sienna FE 2500 Spinning Reel",            "Smooth freshwater spinning reel with front drag. Like new.", 29.99m, true),
        new("Black Diamond Trail Sport Trekking Poles Pair",   "Adjustable aluminum poles with cork grips. Light use.", 79.99m, true),
        new("Jetboil Flash Backpacking Stove Cooking System",  "Boils 500 mL in 100 seconds. Compact and efficient. Good.", 99.99m, true),
        new("GoPro HERO12 Black Action Camera",                "GoPro HERO12 with HyperSmooth 6.0 and 5.3K video. Like new.", 349.99m, true),
        new("Yeti Hopper M30 2.0 Soft Cooler Navy",           "Portable soft cooler with leakproof zipper. Excellent condition.", 349.99m, false),
        new("Thule Crossover 2 Backpack 20L Black",            "Versatile laptop backpack with hardshell compartment. Barely used.", 129.99m, true),
        new("Big Agnes Q-Core SLX Sleeping Pad 25x72 R4.5",   "Ultralight insulated sleeping pad with R-value 4.5. Excellent.", 149.99m, true),
        new("Hydro Flask 32oz Wide Mouth Bottle Pacific",      "Insulated stainless steel bottle with Flex Cap. Like new.", 44.99m, true),
        new("Garmin Fenix 7S Multisport GPS Smartwatch",       "Premium GPS watch with 11-day battery and solar charging.", 549.99m, true),
        new("CAMP Photon Rock Climbing Harness Medium",        "Lightweight sport climbing harness under 200 g. Used one season.", 59.99m, true),
        new("Pelican 45QT Elite Cooler Desert Tan",            "Heavy-duty cooler with freezer-grade insulation. Like new.", 249.99m, false),
    ];

    // ── Cars ──────────────────────────────────────────────────────────────────

    private static readonly LD[] Cars =
    [
        new("2019 Toyota Camry XSE V6 42000 Miles",            "Well-maintained Camry XSE with V6 and sport suspension. Clean title.", 22999.99m, false),
        new("2020 Honda Civic EX 35000 Miles Silver",          "One-owner Civic EX with Honda Sensing suite. Service records included.", 18999.99m, false),
        new("2018 Ford F-150 XLT 4x4 CrewCab 55000 Miles",    "F-150 with 5.0L V8, tow package, and backup camera. Clean title.", 27999.99m, false),
        new("2021 Tesla Model 3 Standard Range Plus White",    "Full self-driving option. 35,000 miles. Home charger included.", 34999.99m, false),
        new("2017 Chevrolet Silverado 1500 LT 4WD 68k",       "Silverado crew cab with Bose audio and heated seats. Good shape.", 24999.99m, false),
        new("2020 Subaru Outback Premium AWD 29000 Miles",     "Outback with EyeSight driver assist. One owner, like new.", 23999.99m, false),
        new("2019 Jeep Wrangler Unlimited Sport 4x4 40k",     "Hardtop JL Wrangler with 3.6L V6. Trail-rated, 40,000 miles.", 29999.99m, false),
        new("2018 BMW 330i xDrive Sedan 48000 Miles",         "3 Series with xDrive AWD, sunroof, and navigation. Excellent.", 28999.99m, false),
        new("2021 Hyundai Tucson SEL AWD 22000 Miles",        "Tucson SEL with AWD, blind-spot monitoring, wireless charging.", 25999.99m, false),
        new("2016 Nissan Frontier Pro-4X 4x4 68000 Miles",   "Pro-4X with Rockford Fosgate audio and skid plates. Off-road ready.", 19999.99m, false),
        new("2022 Mazda CX-5 Touring AWD 15000 Miles",        "CX-5 in Soul Red Crystal. AWD, Bose sound, leather seats.", 27999.99m, false),
        new("2019 Audi Q5 Premium Plus Quattro 38000 mi",     "Quattro AWD, panoramic sunroof, and virtual cockpit. Very clean.", 31999.99m, false),
        new("2020 Dodge Charger R/T Scat Pack V8 32000 mi",  "392 Hemi V8 with 485 hp. Wide Body package. One owner.", 36999.99m, false),
        new("2017 Ford Explorer Limited 4WD 75000 Miles",     "3-row SUV with heated and cooled seats plus SYNC3. Good condition.", 21999.99m, false),
        new("2021 Kia Telluride SX AWD 18000 Miles",          "Telluride SX with leather, surround-view camera, and HUD.", 38999.99m, false),
    ];

    // ── Motorcycles ───────────────────────────────────────────────────────────

    private static readonly LD[] Motorcycles =
    [
        new("2019 Honda CBR500R Sportbike ABS 8000 Miles",     "Honda middleweight sportbike with parallel-twin and ABS. Great shape.", 5499.99m, false),
        new("2020 Yamaha MT-07 Hyper Naked 6000 Miles",        "Aggressive naked bike with 689cc parallel-twin. Excellent condition.", 6999.99m, false),
        new("2018 Harley-Davidson Sportster Iron 883 Dark",    "Custom Dark Iron 883. Low miles, custom grips and foot pegs.", 8999.99m, false),
        new("2021 Kawasaki Z900 ABS 3000 Miles",               "Naked supernaked with 948cc inline-four. Virtually new.", 8499.99m, false),
        new("2019 BMW R1250GS Adventure 12000 Miles",          "BMW adventure tourer with ShiftCam tech. Loaded with accessories.", 12999.99m, false),
        new("2020 Ducati Monster 821 Low Miles Red",           "Italian V-twin naked bike. 821cc Testastretta engine. Stunning.", 9999.99m, false),
        new("2017 Honda CB500X ABS 15000 Miles",               "Versatile adventure-style commuter. Great fuel economy, reliable.", 4999.99m, false),
        new("2021 Royal Enfield Meteor 350 Fireball Red",      "Classic cruiser with 349cc single. Low seat height, beginner friendly.", 4499.99m, false),
        new("2019 KTM Duke 390 ABS 7000 Miles",                "Sporty naked with TFT display and cornering ABS. Great condition.", 4999.99m, false),
        new("2020 Triumph Street Twin 900 5000 Miles",         "Modern classic with 900cc parallel-twin. Light use, excellent.", 7999.99m, false),
        new("2018 Honda Africa Twin DCT 1000cc 20k Miles",     "Africa Twin with Dual Clutch Transmission. Long-distance touring.", 10999.99m, false),
        new("2021 Yamaha XSR900 Retro Sport 2000 Miles",       "Neo-retro sport bike with 890cc CP3 engine. Barely ridden.", 8999.99m, false),
        new("2019 Kawasaki Ninja 400 ABS 9000 Miles",          "Beginner-friendly sportbike with 399cc parallel-twin. Clean title.", 5499.99m, false),
        new("2020 Suzuki V-Strom 650XT Adventure Tan",         "Practical adventure tourer with spoked wheels. Very reliable.", 7499.99m, false),
        new("2017 Harley-Davidson Softail Deluxe FLSTN",       "Big Twin cruiser with chrome accessories. Service records available.", 13999.99m, false),
    ];

    // ── Action Figures ────────────────────────────────────────────────────────

    private static readonly LD[] ActionFigures =
    [
        new("Marvel Legends Spider-Man 6 Inch Figure NIB",     "Marvel Legends Series Spider-Man in classic red-blue suit. New.", 24.99m, true),
        new("Star Wars Black Series Darth Vader 6 Inch",       "Hasbro Black Series Darth Vader with lightsaber. New in box.", 34.99m, true),
        new("NECA TMNT Turtles in Time Raphael 7 Inch",       "NECA arcade-game Raphael in retro 16-bit style. Mint in package.", 49.99m, true),
        new("Hot Toys Iron Man MK50 1/6 Scale Figure",         "Movie Masterpiece MK50 with LED lights and 30+ accessories.", 299.99m, false),
        new("Hasbro GI Joe Classified Snake Eyes 6 Inch",     "GI Joe Classified Series Snake Eyes with full accessory set.", 29.99m, true),
        new("Mezco One 12 Collective Dark Knight Batman",      "Mezco One:12 Collective DKR Batman. Opened, complete set.", 89.99m, true),
        new("Marvel Legends X-Men Retro Wolverine 6 Inch",    "Retro card packaging Wolverine in brown costume. New.", 22.99m, true),
        new("McFarlane DC Multiverse Superman 7 Inch",         "McFarlane Superman with collect-a-figure piece. New in box.", 19.99m, true),
        new("NECA Alien Warrior 7 Inch Action Figure",         "NECA Big Chap Alien Warrior with articulated inner jaw. New.", 34.99m, true),
        new("Hot Toys Avengers Endgame Thor 1/6 Scale",        "DX Thor with light-up eyes and Stormbreaker. Near mint.", 279.99m, false),
        new("Star Wars Black Series Boba Fett Archive 6in",    "Archive Collection Boba Fett with jetpack and blaster. New.", 44.99m, true),
        new("Marvel Legends Deadpool and Wolverine 2-Pack",    "Special 2-pack from the 2024 movie. Both figures new in box.", 39.99m, true),
        new("NECA IT Pennywise Ultimate 7 Inch Figure",        "Ultimate Pennywise with interchangeable heads. New in box.", 29.99m, true),
        new("Funko POP Deluxe Aquaman on Throne",              "Deluxe Funko POP on movie throne base. New in box, great display.", 24.99m, true),
        new("McFarlane Spawn 30th Anniversary 7 Inch",         "Spawn with massive cape and accessories. New on card.", 19.99m, true),
    ];

    // ── Board Games ───────────────────────────────────────────────────────────

    private static readonly LD[] BoardGames =
    [
        new("Catan Board Game 5th Edition Sealed",             "Classic Catan base game factory sealed. Perfect gift.", 44.99m, true),
        new("Ticket to Ride Europe Board Game Complete",       "Ticket to Ride Europe. Complete with all pieces, excellent.", 49.99m, true),
        new("Pandemic Board Game New Factory Sealed",          "Cooperative classic where players race to stop outbreaks.", 39.99m, true),
        new("Codenames Party Game New Sealed",                 "Word-clue party game for 2 to 8 or more players. New.", 19.99m, true),
        new("Wingspan Board Game with Swift Start Pack",       "Engine-building bird game. Complete with all pieces.", 59.99m, true),
        new("Betrayal at House on the Hill 3rd Edition",       "Modular haunted mansion game with 50 scenarios. Complete.", 44.99m, true),
        new("Gloomhaven Jaws of the Lion Board Game",          "Standalone dungeon crawler with 25 scenarios. Complete.", 49.99m, false),
        new("Azul Board Game Next Move Games",                 "Abstract tile-placing game. Complete and in great condition.", 34.99m, true),
        new("Mysterium Park Board Game New Sealed",            "Cooperative deduction game with stunning artwork. New.", 34.99m, true),
        new("Spirit Island Board Game All Expansions",         "Complex cooperative strategy game. All expansions included.", 69.99m, false),
        new("Everdell Board Game with 3D Evertree",            "Worker-placement game with 3D Evertree centerpiece. Complete.", 44.99m, true),
        new("7 Wonders Duel 2-Player Board Game",              "Two-player civilization game from Repos Production. New.", 29.99m, true),
        new("Agricola Family Edition Board Game",              "Simplified Agricola for families. Complete, good condition.", 29.99m, true),
        new("Scythe Board Game Stonemaier All Promos",         "Engine-building alt-history game. All promos included.", 69.99m, false),
        new("Root Board Game Leder Games Base",                "Asymmetric woodland warfare base game. Complete.", 59.99m, true),
    ];

    // ── Fiction Books ─────────────────────────────────────────────────────────

    private static readonly LD[] FictionBooks =
    [
        new("The Name of the Wind Patrick Rothfuss PB",        "Kingkiller Chronicle Book 1. Paperback in great reading condition.", 12.99m, true),
        new("A Court of Thorns and Roses S.J. Maas PB",       "ACOTAR Book 1 by Sarah J. Maas. Light wear, great condition.", 14.99m, true),
        new("The Way of Kings Brandon Sanderson HC",           "Stormlight Archive Book 1. Hardcover, near-fine condition.", 24.99m, true),
        new("Fourth Wing Rebecca Yarros Hardcover",            "Bestselling fantasy romance. Hardcover, read once.", 22.99m, true),
        new("It Ends with Us Colleen Hoover Paperback",        "Emotionally powerful romance novel. Good reading condition.", 12.99m, true),
        new("Project Hail Mary Andy Weir Hardcover",           "Standalone sci-fi survival story. Hardcover first printing.", 19.99m, true),
        new("The Midnight Library Matt Haig Paperback",        "Philosophical fiction about second chances. Like new.", 13.99m, true),
        new("Babel R.F. Kuang Hardcover First Edition",        "Dark academia fantasy set in 1830s Oxford. Near-fine HC.", 15.99m, true),
        new("Tomorrow and Tomorrow Gabrielle Zevin HC",        "Literary fiction about friendship and video games. Excellent.", 19.99m, true),
        new("The Atlas Six Olivie Blake Paperback",            "Dark academia fantasy with unreliable narrators. Good cond.", 14.99m, true),
        new("House of Salt and Sorrows Erin A. Craig PB",     "Gothic horror retelling of the 12 Dancing Princesses.", 11.99m, true),
        new("Words of Radiance Brandon Sanderson HC",          "Stormlight Archive Book 2. Hardcover in great condition.", 26.99m, true),
        new("Piranesi Susanna Clarke Hardcover",               "Mysterious standalone fantasy novel. First edition hardcover.", 17.99m, true),
        new("The Invisible Life of Addie LaRue V.E. Schwab",  "Epic fantasy about immortality and being forgotten. Fine HC.", 22.99m, true),
        new("Klara and the Sun Kazuo Ishiguro Paperback",     "Booker Prize-winning author. Paperback in like-new condition.", 13.99m, true),
    ];

    // ── Academic Textbooks ────────────────────────────────────────────────────

    private static readonly LD[] Textbooks =
    [
        new("Calculus Early Transcendentals 9th Ed Stewart",   "James Stewart Calculus textbook. Minimal highlighting.", 89.99m, false),
        new("Organic Chemistry 9th Edition McMurry Used",      "McMurry Organic Chemistry with solutions guide. Good condition.", 79.99m, false),
        new("Principles of Economics 8th Ed Mankiw",           "Mankiw econ textbook used in top universities. Some highlighting.", 69.99m, true),
        new("Human Anatomy Physiology 11th Ed Marieb",         "Marieb A&P with extra chapters. Lab manual access code unused.", 74.99m, false),
        new("Introduction to Algorithms 4th Ed CLRS",          "CLRS algorithms bible. Hardcover in great condition.", 79.99m, true),
        new("Fundamentals of Nursing 10th Ed Potter Perry",    "Potter & Perry nursing textbook with study guide. Very good.", 94.99m, false),
        new("Physics Scientists Engineers 9th Ed Serway",      "Algebra-based physics with online access. Light use.", 84.99m, false),
        new("Financial Accounting 17th Edition Warren",        "Warren Financial Accounting with CengageNOW access. New.", 74.99m, true),
        new("Chemistry A Molecular Approach 5th Ed Tro",       "Tro Chemistry used in general chemistry courses. Good.", 84.99m, false),
        new("Campbell Biology 12th Edition Reece Urry",        "The standard biology textbook. Good condition, light notes.", 89.99m, false),
        new("Linear Algebra Its Applications 6th Ed Lay",      "Lay linear algebra text. Like new condition, great for study.", 64.99m, true),
        new("Organizational Behavior 18th Ed Robbins",         "Robbins OB textbook for management courses. Very good cond.", 74.99m, false),
        new("Pathophysiology 7th Ed McCance Huether",          "Medical pathophysiology reference. Good condition, some notes.", 59.99m, false),
        new("Discrete Mathematics with Applications Epp",      "Susanna Epp discrete math for CS students. Excellent condition.", 74.99m, true),
        new("Operations Management 14th Ed Heizer Render",     "Heizer operations management. Good for supply chain courses.", 79.99m, false),
    ];

    // ── Trading Cards ─────────────────────────────────────────────────────────

    private static readonly LD[] TradingCards =
    [
        new("Pokemon Pikachu VMAX Rainbow Rare PSA 9",         "Pikachu VMAX Rainbow Rare from Vivid Voltage. PSA 9 graded.", 149.99m, true),
        new("Magic The Gathering Mox Opal MM2 Near Mint",      "Mox Opal from Modern Masters 2015. Near mint, unplayed condition.", 79.99m, true),
        new("2003 LeBron James Topps Chrome RC PSA 8",         "LeBron James Topps Chrome Rookie Card. PSA graded Excellent.", 299.99m, true),
        new("Pokemon Charizard VMAX Champions Path Alt Art",   "Charizard VMAX from Champions Path. Near mint, pulled fresh.", 49.99m, true),
        new("Yu-Gi-Oh Blue-Eyes White Dragon LOB 1st Ed LP",  "Legend of Blue Eyes White Dragon 1st edition. Lightly played.", 199.99m, false),
        new("1989 Ken Griffey Jr Upper Deck RC PSA 9",         "Griffey Jr rookie from 1989 UD set. PSA 9 Mint.", 199.99m, true),
        new("Pokemon Umbreon VMAX Alt Art 215 196 NM",         "Evolving Skies secret rare. Near mint pulled from booster.", 89.99m, true),
        new("2020 Patrick Mahomes Prizm Silver PSA 10",        "Mahomes Silver Prizm gem mint 10. Investment-grade card.", 199.99m, true),
        new("Pokemon Evolving Skies Booster Box Sealed",       "Factory-sealed SWSH Evolving Skies booster box. 36 packs.", 149.99m, false),
        new("1986 Fleer Michael Jordan Rookie Card PSA 8",    "MJ iconic Fleer rookie card. PSA 8 Excellent condition.", 999.99m, true),
        new("Pokemon Silver Tempest Booster Box Sealed",       "Factory-sealed Silver Tempest booster box. 36 packs.", 79.99m, false),
        new("Yu-Gi-Oh Ghost Rare Stardust Dragon 1st Ed",     "Stardust Dragon Ghost Rare 1st Edition. Lightly played.", 149.99m, true),
        new("2021 Topps Finest Football Hobby Box Sealed",     "Sealed hobby box with on-card autographs and refractors.", 199.99m, false),
        new("Pokemon Paldea Evolved Booster Box Sealed",       "Factory-sealed Paldea Evolved booster box. 36 packs.", 89.99m, false),
        new("Magic The Gathering The One Ring Serialized",     "Serialized 1-of-1 variant replica card for display. NM.", 14.99m, true),
    ];

    // ── Coins ─────────────────────────────────────────────────────────────────

    private static readonly LD[] Coins =
    [
        new("2021 American Eagle 1oz Silver Bullion BU",       "US Mint Silver Eagle in brilliant uncirculated condition.", 34.99m, true),
        new("1964 Kennedy Half Dollar 90% Silver MS64",        "Last year of 90% silver Kennedy. Nice MS64 coin.", 24.99m, true),
        new("2023 American Gold Eagle 1/10 oz BU",             "1/10 oz American Gold Eagle from 2023 US Mint. BU grade.", 229.99m, true),
        new("Walking Liberty Half Dollar 1941-D AU58",         "Beautiful 1941-D Walking Liberty. Nearly uncirculated grade.", 49.99m, true),
        new("Morgan Silver Dollar 1880-S MS63 NGC Graded",     "1880-S Morgan Dollar certified MS63 by NGC. Great luster.", 89.99m, true),
        new("2022 Canadian Maple Leaf 1oz Silver BU",          "Royal Canadian Mint 1 oz silver. Brilliant uncirculated.", 32.99m, true),
        new("1955 DDO Lincoln Wheat Penny F-15 ANACS",         "1955 Doubled Die Obverse cent. ANACS F-15 certified key date.", 449.99m, false),
        new("Peace Dollar 1921 High Relief XF-40",             "First-year 1921 Peace Dollar in XF-40 grade. Sharp details.", 79.99m, true),
        new("2021 Morgan Silver Dollar CC Privy MS68 NGC",     "Centennial Morgan with CC privy mark. NGC MS68 top pop.", 89.99m, true),
        new("Mercury Dime Set 1916-1945 VG Average Grade",     "Full 76-coin Mercury Dime set in album. No 1916-D included.", 199.99m, false),
        new("1899 Indian Head Cent MS63 Red Brown PCGS",       "1899 Indian Cent in MS63 Red Brown. PCGS certified.", 149.99m, true),
        new("2023 US Mint Silver Proof Set 10 Coins Sealed",   "Complete 2023 Silver Proof Set from the US Mint. Sealed.", 79.99m, true),
        new("1924 Saint-Gaudens Double Eagle VF-30 NGC",       "Iconic pre-33 gold $20 coin. NGC VF-30. Investment grade.", 1499.99m, false),
        new("Seated Liberty Quarter 1876-CC G-04 NGC",         "Carson City Seated Liberty Quarter. NGC G-04 certified.", 249.99m, true),
        new("2022 South Africa Krugerrand 1oz Gold BU",        "Classic South African 1 oz gold coin. Brilliant uncirculated.", 1999.99m, false),
    ];

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Guid CreateGuid(string value)
    {
        var bytes = MD5.HashData(Encoding.UTF8.GetBytes(value));
        return new Guid(bytes);
    }
}
