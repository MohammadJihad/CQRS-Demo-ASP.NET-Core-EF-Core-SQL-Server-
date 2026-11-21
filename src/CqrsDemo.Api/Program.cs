using CqrsDemo.Api.Endpoints;
using CqrsDemo.Api.ErrorHandling;
using CqrsDemo.Application.Products.Commands.AdjustProductStock;
using CqrsDemo.Application.Products.Commands.CreateProduct;
using CqrsDemo.Application.Products.Commands.DeleteProduct;
using CqrsDemo.Application.Products.Commands.UpdateProduct;
using CqrsDemo.Application.Products.Queries.GetProductById;
using CqrsDemo.Application.Products.Queries.GetProducts;
using CqrsDemo.Infrastructure;
using CqrsDemo.Infrastructure.Persistence;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddSingleton(TimeProvider.System);

// Handlers are plain classes registered with the built-in container. No MediatR is needed:
// each endpoint asks for exactly the handler it uses, so the CQRS flow stays visible.
builder.Services.AddScoped<CreateProductHandler>();
builder.Services.AddScoped<UpdateProductHandler>();
builder.Services.AddScoped<AdjustProductStockHandler>();
builder.Services.AddScoped<DeleteProductHandler>();
builder.Services.AddScoped<GetProductByIdHandler>();
builder.Services.AddScoped<GetProductsHandler>();

builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
builder.Services.AddOpenApi();

var app = builder.Build();

// One-off maintenance mode: dotnet run --project src/CqrsDemo.Api -- --seed-sample-data
if (args.Contains("--seed-sample-data"))
{
    await using var scope = app.Services.CreateAsyncScope();
    var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
    var inserted = await SampleDataSeeder.SeedAsync(db, TimeProvider.System);
    Console.WriteLine($"Sample data: {inserted} product(s) inserted.");
    return;
}

app.UseExceptionHandler();
app.UseStatusCodePages();

if (app.Environment.IsDevelopment())
{
    // OpenAPI document at /openapi/v1.json
    app.MapOpenApi();
}

app.MapProductEndpoints();

app.Run();

// Lets the test project use WebApplicationFactory<Program>.
public partial class Program;
