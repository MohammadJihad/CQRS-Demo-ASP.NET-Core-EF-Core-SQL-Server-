namespace CqrsDemo.Application.Products.Queries.GetProducts;

/// <summary>Question: "which products are there (optionally matching a name), one page at a time?"</summary>
/// <param name="Page">1-based page number.</param>
/// <param name="PageSize">Items per page, 1 to <see cref="MaxPageSize"/>.</param>
/// <param name="Name">Optional filter: products whose name contains this text.</param>
public sealed record GetProductsQuery(int Page = 1, int PageSize = GetProductsQuery.DefaultPageSize, string? Name = null)
{
    public const int DefaultPageSize = 10;
    public const int MaxPageSize = 100;
}
