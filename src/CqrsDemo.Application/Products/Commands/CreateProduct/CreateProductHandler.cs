using CqrsDemo.Application.Abstractions;
using CqrsDemo.Domain.Products;

namespace CqrsDemo.Application.Products.Commands.CreateProduct;

/// <summary>
/// Handles <see cref="CreateProductCommand"/>: validates through the domain, inserts, and returns the new id.
/// </summary>
/// <remarks>
/// <para>
/// Returning the id is fine. CQRS doesn't require commands to return nothing. It requires them
/// not to double as queries. The caller gets the identifier, which it needs to build the Location
/// header, and fetches the full product through the GetProductById query when it needs it.
/// </para>
/// <para>
/// TimeProvider is injected instead of calling DateTime.UtcNow so tests can control the clock.
/// </para>
/// </remarks>
public sealed class CreateProductHandler(IWriteDbContext db, TimeProvider timeProvider)
{
    public async Task<Guid> HandleAsync(CreateProductCommand command, CancellationToken cancellationToken)
    {
        // Business rules run here, inside the entity. If any rule fails, Product.Create throws
        // DomainValidationException and no product is ever added to the DbContext.
        var product = Product.Create(
            command.Name,
            command.Description,
            command.Price,
            command.StockQuantity,
            timeProvider.GetUtcNow().UtcDateTime);

        // Add only registers the entity with the change tracker; nothing is sent to SQL Server yet.
        db.Products.Add(product);

        // One INSERT statement, executed in its own transaction.
        await db.SaveChangesAsync(cancellationToken);

        return product.Id;
    }
}
