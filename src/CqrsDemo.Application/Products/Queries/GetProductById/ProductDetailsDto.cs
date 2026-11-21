namespace CqrsDemo.Application.Products.Queries.GetProductById;

/// <summary>
/// Read model for a product details page.
/// </summary>
/// <remarks>
/// A DTO (data transfer object) has no behavior and no rules; it is just the shape the caller
/// wants. Note <see cref="IsInStock"/>: it doesn't exist in the table or on the entity. The query
/// computes it in SQL. That is a small example of a read model differing from the write model
/// even though both use the same table.
/// </remarks>
public sealed record ProductDetailsDto(
    Guid Id,
    string Name,
    string? Description,
    decimal Price,
    int StockQuantity,
    bool IsInStock,
    DateTime CreatedAtUtc);
