using CqrsDemo.Application.Abstractions;
using CqrsDemo.Application.Common;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Products.Commands.AdjustProductStock;

/// <summary>Handles <see cref="AdjustProductStockCommand"/> and reports the before/after quantity.</summary>
public sealed class AdjustProductStockHandler(IWriteDbContext db)
{
    public async Task<StockAdjustmentResult> HandleAsync(AdjustProductStockCommand command, CancellationToken cancellationToken)
    {
        var product = await db.Products.SingleOrDefaultAsync(p => p.Id == command.ProductId, cancellationToken)
            ?? throw new NotFoundException("Product", command.ProductId);

        var previousQuantity = product.StockQuantity;

        // The "stock cannot go negative" rule is enforced here, in the entity.
        product.AdjustStock(command.QuantityChange);

        try
        {
            // Race condition guard: two requests could both read stock = 5 and both remove 4.
            // The rowversion check makes the second SaveChanges fail instead of writing 1 over 1
            // (a lost update), so stock can never be oversold. See docs/09-tradeoffs-and-mistakes.md.
            await db.SaveChangesAsync(cancellationToken);
        }
        catch (DbUpdateConcurrencyException ex)
        {
            throw new ConcurrencyConflictException("Product", command.ProductId, ex);
        }

        return new StockAdjustmentResult(product.Id, previousQuantity, product.StockQuantity);
    }
}
