namespace CqrsDemo.Application.Products.Commands.AdjustProductStock;

/// <summary>
/// Intent: "N units arrived (positive) or left (negative)."
/// </summary>
/// <remarks>
/// The command carries a <i>change</i>, not a new absolute value. "Remove 3" says what happened
/// in the business; "set stock to 7" would silently overwrite anyone else's adjustment.
/// </remarks>
public sealed record AdjustProductStockCommand(Guid ProductId, int QuantityChange);
