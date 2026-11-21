# 5. Debugging breakpoints

Run the API under the debugger (`dotnet run` + attach, or F5 on `CqrsDemo.Api`), then send requests from `src/CqrsDemo.Api/CqrsDemo.http`.

| Scenario | File | Method |
|---|---|---|
| Create entry | src/CqrsDemo.Api/Endpoints/ProductEndpoints.cs | `CreateProduct` |
| Create logic | src/CqrsDemo.Application/Products/Commands/CreateProduct/CreateProductHandler.cs | `HandleAsync` |
| Business rules | src/CqrsDemo.Domain/Products/Product.cs | `Create`, `UpdateDetails`, `AdjustStock` |
| Persistence | src/CqrsDemo.Infrastructure/Persistence/CatalogDbContext.cs | `SaveChangesAsync` (framework) / `ProductConfiguration.Configure` |
| List query | src/CqrsDemo.Application/Products/Queries/GetProducts/GetProductsHandler.cs | `HandleAsync`, `Validate` |
| Get by id | src/CqrsDemo.Application/Products/Queries/GetProductById/GetProductByIdHandler.cs | `HandleAsync` |
| Concurrency | src/CqrsDemo.Application/Products/Commands/AdjustProductStock/AdjustProductStockHandler.cs | `HandleAsync` (catch block) |
| Error mapping | src/CqrsDemo.Api/ErrorHandling/ProblemDetailsExceptionHandler.cs | `TryHandleAsync` |

Tips: inspect `db.ChangeTracker.Entries()` in a command (one tracked entity) and in a query (none). Enable EF SQL logging (`Microsoft.EntityFrameworkCore.Database.Command: Information`) to see the exact SQL.
