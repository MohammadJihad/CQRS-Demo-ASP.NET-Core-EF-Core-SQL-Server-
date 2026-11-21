using CqrsDemo.Application.Common;
using CqrsDemo.Application.Products.Commands.AdjustProductStock;
using CqrsDemo.Application.Products.Commands.CreateProduct;
using CqrsDemo.Application.Products.Commands.DeleteProduct;
using CqrsDemo.Application.Products.Commands.UpdateProduct;
using CqrsDemo.Application.Products.Queries.GetProductById;
using CqrsDemo.Application.Products.Queries.GetProducts;

namespace CqrsDemo.Api.Endpoints;

/// <summary>
/// HTTP surface of the sample. Each endpoint receives exactly the handler it needs as a parameter
/// (no mediator): read the route to see which command or query runs.
/// </summary>
public static class ProductEndpoints
{
    // Request bodies carry only what the caller decides; the route supplies the product id.
    public sealed record CreateProductRequest(string? Name, string? Description, decimal Price, int StockQuantity);

    public sealed record UpdateProductRequest(string? Name, string? Description, decimal Price);

    public sealed record AdjustStockRequest(int QuantityChange);

    public sealed record CreatedProductResponse(Guid Id);

    public static IEndpointRouteBuilder MapProductEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/products").WithTags("Products");

        // ---- Queries (read side) ----

        group.MapGet("/", GetProducts)
            .WithName("GetProducts")
            .Produces<PagedResult<ProductSummaryDto>>()
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapGet("/{id:guid}", GetProductById)
            .WithName("GetProductById")
            .Produces<ProductDetailsDto>()
            .ProducesProblem(StatusCodes.Status404NotFound);

        // ---- Commands (write side) ----

        group.MapPost("/", CreateProduct)
            .WithName("CreateProduct")
            .Produces<CreatedProductResponse>(StatusCodes.Status201Created)
            .ProducesProblem(StatusCodes.Status400BadRequest);

        group.MapPut("/{id:guid}", UpdateProduct)
            .WithName("UpdateProduct")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapPost("/{id:guid}/stock-adjustments", AdjustStock)
            .WithName("AdjustProductStock")
            .Produces<StockAdjustmentResult>()
            .ProducesProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        group.MapDelete("/{id:guid}", DeleteProduct)
            .WithName("DeleteProduct")
            .Produces(StatusCodes.Status204NoContent)
            .ProducesProblem(StatusCodes.Status404NotFound)
            .ProducesProblem(StatusCodes.Status409Conflict);

        return app;
    }

    private static async Task<IResult> GetProducts(
        GetProductsHandler handler,
        CancellationToken cancellationToken,
        int page = 1,
        int pageSize = GetProductsQuery.DefaultPageSize,
        string? name = null) =>
        Results.Ok(await handler.HandleAsync(new GetProductsQuery(page, pageSize, name), cancellationToken));

    private static async Task<IResult> GetProductById(
        Guid id,
        GetProductByIdHandler handler,
        CancellationToken cancellationToken)
    {
        var product = await handler.HandleAsync(new GetProductByIdQuery(id), cancellationToken);

        // A query answers "nothing there" with null; translating that to 404 is the API's job.
        return product is null
            ? Results.Problem(
                statusCode: StatusCodes.Status404NotFound,
                title: "Resource not found.",
                detail: $"Product '{id}' was not found.")
            : Results.Ok(product);
    }

    private static async Task<IResult> CreateProduct(
        CreateProductRequest request,
        CreateProductHandler handler,
        CancellationToken cancellationToken)
    {
        var id = await handler.HandleAsync(
            new CreateProductCommand(request.Name, request.Description, request.Price, request.StockQuantity),
            cancellationToken);

        // 201 + Location header pointing at the GetProductById endpoint.
        return Results.CreatedAtRoute("GetProductById", new { id }, new CreatedProductResponse(id));
    }

    private static async Task<IResult> UpdateProduct(
        Guid id,
        UpdateProductRequest request,
        UpdateProductHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new UpdateProductCommand(id, request.Name, request.Description, request.Price), cancellationToken);
        return Results.NoContent();
    }

    private static async Task<IResult> AdjustStock(
        Guid id,
        AdjustStockRequest request,
        AdjustProductStockHandler handler,
        CancellationToken cancellationToken) =>
        Results.Ok(await handler.HandleAsync(new AdjustProductStockCommand(id, request.QuantityChange), cancellationToken));

    private static async Task<IResult> DeleteProduct(
        Guid id,
        DeleteProductHandler handler,
        CancellationToken cancellationToken)
    {
        await handler.HandleAsync(new DeleteProductCommand(id), cancellationToken);
        return Results.NoContent();
    }
}
