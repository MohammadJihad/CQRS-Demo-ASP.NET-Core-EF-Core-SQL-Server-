namespace CqrsDemo.Application.Products.Commands.DeleteProduct;

/// <summary>Intent: "remove this product from the catalog."</summary>
public sealed record DeleteProductCommand(Guid Id);
