using CqrsDemo.Application.Abstractions;
using CqrsDemo.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Products.Commands.UpdateProduct;

/// <summary>Handles <see cref="UpdateProductCommand"/>. Returns nothing: success means "done".</summary>
public sealed class UpdateProductHandler(IWriteDbContext db)
{
    public async Task HandleAsync(UpdateProductCommand command, CancellationToken cancellationToken)
    {
        // Commands load the full, *tracked* entity (no AsNoTracking): the change tracker needs
        // to see the modification to generate the UPDATE statement.
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == command.Id, cancellationToken)
            ?? throw new NotFoundException("Product", command.Id);

        // Business rules run inside the entity; on failure the entity is left unchanged.
        product.UpdateDetails(command.Name, command.Description, command.Price);

        try
        {
            // EF Core sends an UPDATE for the changed columns only, with
            // "WHERE Id = @id AND RowVersion = @originalRowVersion" for optimistic concurrency.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Product", command.Id, ex);
        }
    }
}
