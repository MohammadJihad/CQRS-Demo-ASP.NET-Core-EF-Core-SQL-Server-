using CqrsDemo.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Abstractions;

/// <summary>
/// What a <b>command</b> handler is allowed to do with the database: load tracked entities,
/// add or remove them, and save changes.
/// </summary>
/// <remarks>
/// <para>
/// Why an interface at all? The Application project must not reference Infrastructure
/// (Infrastructure references Application, and a reference back would be circular).
/// CatalogDbContext in Infrastructure implements this interface, and dependency injection
/// hands the same instance to the handlers.
/// </para>
/// <para>
/// This is <i>not</i> a generic repository. It exposes EF Core's own DbSet, so handlers keep
/// full LINQ and change tracking, and nothing duplicates what EF Core already provides.
/// </para>
/// </remarks>
public interface IWriteDbContext
{
    DbSet<Product> Products { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}
