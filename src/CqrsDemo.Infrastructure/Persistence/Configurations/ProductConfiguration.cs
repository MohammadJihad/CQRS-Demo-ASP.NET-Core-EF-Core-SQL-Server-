using CqrsDemo.Domain.Products;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace CqrsDemo.Infrastructure.Persistence.Configurations;

/// <summary>
/// Maps <see cref="Product"/> to the dbo.Products table. Database details stay here so the
/// Domain project remains free of persistence attributes.
/// </summary>
internal sealed class ProductConfiguration : IEntityTypeConfiguration<Product>
{
    /// <summary>Shadow property: exists in the table and EF model, but not on the domain entity.</summary>
    public const string RowVersionProperty = "RowVersion";

    public void Configure(EntityTypeBuilder<Product> builder)
    {
        builder.ToTable("Products", table =>
        {
            // Defense in depth: the Domain already enforces these rules, but a CHECK constraint
            // protects the data from anything that bypasses the application (manual SQL, other apps).
            table.HasCheckConstraint("CK_Products_Price_NonNegative", "[Price] >= 0");
            table.HasCheckConstraint("CK_Products_StockQuantity_NonNegative", "[StockQuantity] >= 0");
        });

        builder.HasKey(p => p.Id);

        // The domain generates ids (Product.Create), so the database must not.
        builder.Property(p => p.Id).ValueGeneratedNever();

        builder.Property(p => p.Name)
            .HasMaxLength(Product.NameMaxLength)
            .IsRequired();

        builder.Property(p => p.Description)
            .HasMaxLength(Product.DescriptionMaxLength);

        builder.Property(p => p.Price)
            .HasPrecision(18, 2);

        // SQL Server datetime2 doesn't store DateTimeKind. Without this converter, values read
        // back would be DateTimeKind.Unspecified and serialize without the trailing "Z".
        builder.Property(p => p.CreatedAtUtc)
            .HasConversion(v => v, v => DateTime.SpecifyKind(v, DateTimeKind.Utc));

        // Optimistic concurrency token. SQL Server changes a rowversion value automatically on
        // every UPDATE. EF Core adds "AND RowVersion = @original" to UPDATE/DELETE statements and
        // throws DbUpdateConcurrencyException when no row matched, meaning someone else changed it first.
        builder.Property<byte[]>(RowVersionProperty)
            .IsRowVersion();

        // Supports ORDER BY Name for the paged list. A LIKE '%term%' filter can't seek on an index;
        // see docs/09-tradeoffs-and-mistakes.md.
        builder.HasIndex(p => p.Name);
    }
}
