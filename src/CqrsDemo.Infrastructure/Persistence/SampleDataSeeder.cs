using CqrsDemo.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Infrastructure.Persistence;

/// <summary>
/// Inserts a fixed set of sample products. Safe to run any number of times.
/// </summary>
/// <remarks>
/// Each sample product has a hard-coded id. The seeder asks the database which of those ids
/// already exist and inserts only the missing ones, so repeated runs never duplicate data.
/// It never deletes or updates rows: sample products you edited keep your edits.
/// Run it with: dotnet run --project src/CqrsDemo.Api -- --seed-sample-data
/// </remarks>
public static class SampleDataSeeder
{
    private sealed record SampleProduct(Guid Id, string Name, string Description, decimal Price, int StockQuantity);

    private static readonly SampleProduct[] Samples =
    [
        new(Guid.Parse("0b5f1c1e-0001-4c3a-9a51-5a3b9f0c0001"), "Mechanical Keyboard", "Tenkeyless keyboard with hot-swappable switches.", 89.99m, 25),
        new(Guid.Parse("0b5f1c1e-0002-4c3a-9a51-5a3b9f0c0002"), "Wireless Mouse", "Ergonomic mouse with a 70-day battery life.", 39.50m, 60),
        new(Guid.Parse("0b5f1c1e-0003-4c3a-9a51-5a3b9f0c0003"), "Gaming Mouse", "Lightweight wired mouse with adjustable DPI.", 54.00m, 0),
        new(Guid.Parse("0b5f1c1e-0004-4c3a-9a51-5a3b9f0c0004"), "Mouse Pad XL", "Extended desk mat, 900 x 400 mm.", 19.99m, 120),
        new(Guid.Parse("0b5f1c1e-0005-4c3a-9a51-5a3b9f0c0005"), "27-inch Monitor", "1440p IPS panel with a height-adjustable stand.", 279.00m, 8),
        new(Guid.Parse("0b5f1c1e-0006-4c3a-9a51-5a3b9f0c0006"), "USB-C Dock", "Dual-display dock with 100 W power delivery.", 149.00m, 15),
        new(Guid.Parse("0b5f1c1e-0007-4c3a-9a51-5a3b9f0c0007"), "Noise-Cancelling Headphones", "Over-ear Bluetooth headphones.", 199.95m, 12),
        new(Guid.Parse("0b5f1c1e-0008-4c3a-9a51-5a3b9f0c0008"), "HD Webcam", "1080p webcam with a privacy shutter.", 59.00m, 30),
        new(Guid.Parse("0b5f1c1e-0009-4c3a-9a51-5a3b9f0c0009"), "Laptop Stand", "Aluminium stand, six height positions.", 34.90m, 45),
        new(Guid.Parse("0b5f1c1e-0010-4c3a-9a51-5a3b9f0c0010"), "USB Microphone", "Cardioid condenser microphone for calls and streaming.", 99.00m, 0),
        new(Guid.Parse("0b5f1c1e-0011-4c3a-9a51-5a3b9f0c0011"), "Desk Lamp", "LED lamp with adjustable colour temperature.", 42.00m, 22),
        new(Guid.Parse("0b5f1c1e-0012-4c3a-9a51-5a3b9f0c0012"), "Cable Organizer", "Set of 10 reusable cable ties.", 7.49m, 300),
    ];

    /// <returns>The number of products inserted (0 when everything already existed).</returns>
    public static async Task<int> SeedAsync(CatalogDbContext db, TimeProvider timeProvider, CancellationToken cancellationToken = default)
    {
        // The seeder never creates or migrates the schema; applying migrations stays an explicit step.
        var pending = await db.Database.GetPendingMigrationsAsync(cancellationToken);
        if (pending.Any())
        {
            throw new InvalidOperationException(
                "The database schema is not up to date. Apply migrations first: " +
                "dotnet ef database update --project src/CqrsDemo.Infrastructure --startup-project src/CqrsDemo.Api");
        }

        var sampleIds = Samples.Select(s => s.Id).ToArray();
        var existingIds = await db.Products
            .Where(p => sampleIds.Contains(p.Id))
            .Select(p => p.Id)
            .ToListAsync(cancellationToken);

        var createdAtUtc = timeProvider.GetUtcNow().UtcDateTime;
        var missing = Samples.Where(s => !existingIds.Contains(s.Id)).ToList();

        foreach (var sample in missing)
        {
            // Sample data goes through the same business rules as API input.
            db.Products.Add(Product.Create(sample.Id, sample.Name, sample.Description, sample.Price, sample.StockQuantity, createdAtUtc));
        }

        await db.SaveChangesAsync(cancellationToken);
        return missing.Count;
    }
}
