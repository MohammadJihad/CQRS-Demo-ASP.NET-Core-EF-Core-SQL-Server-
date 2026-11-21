namespace CqrsDemo.Application.Products.Queries.GetProducts;

/// <summary>
/// Read model for one row in a product list.
/// </summary>
/// <remarks>
/// It's deliberately smaller than <c>ProductDetailsDto</c>: a list page doesn't need the
/// description (which can be up to 2,000 characters), so the query doesn't read that column.
/// Each query gets the shape its caller needs. That freedom is the main payoff of CQRS's read side.
/// </remarks>
public sealed record ProductSummaryDto(
    Guid Id,
    string Name,
    decimal Price,
    int StockQuantity,
    bool IsInStock);
