using AssetManagement.API.Models;
using AssetManagement.API.Services;
using AssetManagement.API.Enums;

namespace AssetManagement.API.Data;

public static class DbSeeder
{
    public static async Task SeedData(ApplicationDbContext context, IPasswordService passwordService)
    {
        // Check if data already exists
        if (context.Users.Any())
        {
            return; // Data already seeded
        }

        var now = DateTime.UtcNow;

        // Create categories first
        var categories = new List<Category>
        {
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Laptop",
                Description = "Portable computers and notebooks",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Phone",
                Description = "Mobile phones and smartphones",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Monitor",
                Description = "Computer displays and screens",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Tablet",
                Description = "Tablet computers and iPads",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Desktop",
                Description = "Desktop computers and workstations",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            },
            new Category
            {
                Id = Guid.NewGuid(),
                Name = "Other",
                Description = "Miscellaneous equipment and accessories",
                Status = CategoryStatus.ACTIVE,
                CreatedAt = now,
                UpdatedAt = now
            }
        };

        context.Categories.AddRange(categories);
        await context.SaveChangesAsync(); // Save categories first to get their IDs

        // Create admin user
        var adminUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "admin@gmail.com",
            PasswordHash = passwordService.HashPassword("Admin@123"),
            FirstName = "Admin",
            LastName = "User",
            Role = UserRole.Admin,
            CreatedAt = now,
            UpdatedAt = now
        };

        // Create regular user
        var regularUser = new User
        {
            Id = Guid.NewGuid(),
            Email = "user@gmail.com",
            PasswordHash = passwordService.HashPassword("User@123"),
            FirstName = "Regular",
            LastName = "User",
            Role = UserRole.User,
            CreatedAt = now,
            UpdatedAt = now
        };

        context.Users.AddRange(adminUser, regularUser);

        // Create sample assets with explicit UTC DateTime handling
        var laptopCategory = categories.First(c => c.Name == "Laptop");
        var phoneCategory = categories.First(c => c.Name == "Phone");
        var monitorCategory = categories.First(c => c.Name == "Monitor");
        var tabletCategory = categories.First(c => c.Name == "Tablet");
        var desktopCategory = categories.First(c => c.Name == "Desktop");

        var assets = new List<Asset>
        {
            CreateAsset("MacBook Pro 16-inch", laptopCategory.Id, "MBP-2023-001", "2023-01-15", "macbook-pro-16-2023.jpg", now),
            CreateAsset("Dell XPS 13", laptopCategory.Id, "DLL-2023-002", "2023-02-20","dell-xps-13-2023.jpg", now),
            CreateAsset("iPhone 15 Pro", phoneCategory.Id, "IPH-2023-003", "2023-03-10", "iphone-15-pro-2023.jpg", now),
            CreateAsset("Samsung Galaxy S24", phoneCategory.Id, "SMS-2023-004", "2023-03-15", "samsung-galaxy-s24-2024.jpg", now),
            CreateAsset("Dell UltraSharp 27-inch Monitor", monitorCategory.Id, "MON-2023-005", "2023-01-30", "dell-ultrasharp-27-2023.jpg", now),
            CreateAsset("iPad Pro 12.9-inch", tabletCategory.Id, "TAB-2023-006", "2023-02-25", "ipad-pro-12-9-2023.jpg", now),
            CreateAsset("HP EliteDesk Desktop", desktopCategory.Id, "DSK-2023-007", "2023-01-10", "hp-elitedesk-2023.jpg", now)
        };

        context.Assets.AddRange(assets);

        await context.SaveChangesAsync();
    }

    private static Asset CreateAsset(string name, Guid categoryId, string serialNumber, string purchaseDateStr, string imageUrl, DateTime now)
    {
        var purchaseDate = DateTime.Parse(purchaseDateStr);
        var utcPurchaseDate = DateTime.SpecifyKind(purchaseDate, DateTimeKind.Utc);

        return new Asset
        {
            Id = Guid.NewGuid(),
            Name = name,
            CategoryId = categoryId,
            SerialNumber = serialNumber,
            PurchaseDate = utcPurchaseDate,
            ImageUrl = imageUrl,
            Status = AssetStatus.Available,
            CreatedAt = now,
            UpdatedAt = now
        };
    }
}
