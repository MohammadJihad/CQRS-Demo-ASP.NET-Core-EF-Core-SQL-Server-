using CqrsDemo.Domain.Products;

namespace CqrsDemo.Application.Abstractions;

/// <summary>
/// What a <b>query</b> handler is allowed to do with the database: read.
/// </summary>
/// <remarks>
/// Exposing only IQueryable, with no SaveChangesAsync, makes the CQRS rule
/// "queries do not change state" something the compiler checks, not just a convention.
/// The same CatalogDbContext instance implements both this interface and IWriteDbContext:
/// it's one database and one DbContext, viewed through two narrow interfaces.
/// </remarks>
public interface IReadDbContext
{
    IQueryable<Product> Products { get; }
}
