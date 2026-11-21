namespace CqrsDemo.Application.Products.Commands.AdjustProductStock;

/// <summary>
/// The outcome of a stock adjustment: a small result that describes what the command did.
/// It is not a read model of the product.
/// </summary>
public sealed record StockAdjustmentResult(Guid ProductId, int PreviousQuantity, int NewQuantity);
