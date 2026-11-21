namespace CqrsDemo.Application.Products.Commands.UpdateProduct;

/// <summary>Intent: "replace this product's name, description, and price."</summary>
/// <remarks>Stock isn't included. It changes only through <c>AdjustProductStockCommand</c>.</remarks>
public sealed record UpdateProductCommand(
    Guid Id,
    string? Name,
    string? Description,
    decimal Price);
