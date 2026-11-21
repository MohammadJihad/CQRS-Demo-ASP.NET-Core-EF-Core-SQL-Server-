using CqrsDemo.Application.Abstractions;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Products.Queries.GetProductById;

/// <summary>Handles <see cref="GetProductByIdQuery"/>. Returns null when the product doesn't exist.</summary>
/// <remarks>
/// Compare this with the command handlers: no entity methods, no business rules, no SaveChanges.
/// The handler doesn't even have access to SaveChanges, because IReadDbContext doesn't expose it.
/// </remarks>
public sealed class GetProductByIdHandler(IReadDbContext db)
{
    public Task<ProductDetailsDto?> HandleAsync(GetProductByIdQuery query, CancellationToken cancellationToken) =>
        db.Products
            // AsNoTracking: don't register results with the change tracker; they will never be saved.
            // (A projection to a DTO is not tracked anyway; calling it states the intent and protects
            // the query if someone later changes it to return entities.)
            .AsNoTracking()
            .Where(p => p.Id == query.Id)
            // Projection: SQL Server returns only these columns, mapped straight into the DTO.
            // The Product entity is never created, so its rules and private setters play no part.
            .Select(p => new ProductDetailsDto(
                p.Id,
                p.Name,
                p.Description,
                p.Price,
                p.StockQuantity,
                p.StockQuantity > 0,
                p.CreatedAtUtc))
            .SingleOrDefaultAsync(cancellationToken);
}
