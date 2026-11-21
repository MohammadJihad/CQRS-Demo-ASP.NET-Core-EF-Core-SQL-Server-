using CqrsDemo.Application.Common;
using CqrsDemo.Application.Products.Queries.GetProducts;

namespace CqrsDemo.Tests.Unit;

[Trait("Category", "Unit")]
public class GetProductsValidationTests
{
    // Validation runs before the handler touches the database, so no database is needed here.
    private static readonly GetProductsHandler Handler = new(null!);

    [Theory]
    [InlineData(0, 10, "page")]
    [InlineData(-1, 10, "page")]
    [InlineData(1, 0, "pageSize")]
    [InlineData(1, -5, "pageSize")]
    [InlineData(1, GetProductsQuery.MaxPageSize + 1, "pageSize")]
    [InlineData(int.MaxValue, GetProductsQuery.MaxPageSize, "page")]
    public async Task Invalid_paging_is_rejected_before_any_query_runs(int page, int pageSize, string expectedField)
    {
        var ex = await Assert.ThrowsAsync<RequestValidationException>(() =>
            Handler.HandleAsync(new GetProductsQuery(page, pageSize), CancellationToken.None));

        Assert.Contains(expectedField, ex.Errors.Keys);
    }

    [Fact]
    public async Task Overlong_name_filter_is_rejected()
    {
        var query = new GetProductsQuery(Name: new string('x', 201));

        var ex = await Assert.ThrowsAsync<RequestValidationException>(() => Handler.HandleAsync(query, CancellationToken.None));

        Assert.Contains("name", ex.Errors.Keys);
    }
}
