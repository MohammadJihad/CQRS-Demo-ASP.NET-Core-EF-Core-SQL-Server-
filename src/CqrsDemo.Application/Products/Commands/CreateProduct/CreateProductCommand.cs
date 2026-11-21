namespace CqrsDemo.Application.Products.Commands.CreateProduct;

/// <summary>
/// Intent: "add this product to the catalog."
/// </summary>
/// <remarks>
/// A command is a plain data object named after a business action. It carries only what the
/// caller decides. Id and CreatedAtUtc are missing on purpose: the system assigns them.
/// </remarks>
public sealed record CreateProductCommand(
    string? Name,
    string? Description,
    decimal Price,
    int StockQuantity);
