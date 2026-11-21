using CqrsDemo.Application.Abstractions;
using CqrsDemo.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Products.Commands.DeleteProduct;

/// <summary>Handles <see cref="DeleteProductCommand"/>.</summary>
/// <remarks>
/// The entity is loaded before removal (rather than a single ExecuteDeleteAsync statement) so a
/// future rule such as "products with stock cannot be deleted" has an obvious home. The cost is
/// one extra SELECT, which is fine for a write path.
/// </remarks>
public sealed class DeleteProductHandler(IWriteDbContext db)
{
    public async Task HandleAsync(DeleteProductCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Product", command.Id);

        db.Products.Remove(product);

        try
        {
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Product", command.Id, ex);
        }
    }
}
