using CqrsDemo.Application.Abstractions;
using CqrsDemo.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Infrastructure.Persistence;

/// <summary>
/// The single EF Core DbContext for the single learning database.
/// </summary>
/// <remarks>
/// It implements both <see cref="IWriteDbContext"/> (used by command handlers) and
/// <see cref="IReadDbContext"/> (used by query handlers). CQRS separates the <i>code paths</i>;
/// it doesn't require separate databases or separate DbContexts.
/// </remarks>
public sealed class CatalogDbContext(DbContextOptions<CatalogDbContext> options) : DbContext(options), IWriteDbContext, IReadDbContext
{
    /// <summary>Name of the connection string in configuration ("ConnectionStrings:CatalogDb").</summary>
    public const string ConnectionStringName = "CatalogDb";

    public DbSet<Product> Products => Set<Product>();

    // Query handlers see the same table as a read-only IQueryable. The explicit interface
    // implementation lets one class satisfy both interfaces with the same property name.
    IQueryable<Product> IReadDbContext.Products => Products;

    protected override void OnModelCreating(ModelBuilder modelBuilder) =>
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(CatalogDbContext).Assembly);
}
