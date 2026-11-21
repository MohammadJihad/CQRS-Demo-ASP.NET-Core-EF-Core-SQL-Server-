# CQRS Demo (ASP.NET Core + EF Core + SQL Server)

A small learning sample of **Command Query Responsibility Segregation**: one database, one `CatalogDbContext`, plain handler classes injected straight into minimal-API endpoints. No MediatR, Dapper, messaging or event sourcing.

## Prerequisites
- .NET SDK **10.0.401** (pinned in `global.json`). EF Core / ASP.NET Core packages **10.0.15**, xunit.v3 4.0.3.
- SQL Server: either Docker (compose file below) or an existing instance / LocalDB.
- `nuget.config` restricts restore to nuget.org (avoids broken machine-wide feeds).

## Database and secrets
No connection string is stored in source. Provide `ConnectionStrings:CatalogDb` via user secrets **or** an environment variable.

Option A – Docker (needs an x64/amd64 host, or Apple Silicon with x86 emulation):
```powershell
Copy-Item .env.example .env        # then edit MSSQL_SA_PASSWORD (placeholder only in the example)
docker compose up -d --wait        # waits for the sqlcmd healthcheck
```
Option B – existing SQL Server / LocalDB: skip Docker and use its connection string. Use a dedicated database such as `CqrsDemoLearning`.

```powershell
dotnet user-secrets set "ConnectionStrings:CatalogDb" "<your connection string>" --project src/CqrsDemo.Api
# or: $env:ConnectionStrings__CatalogDb = "<your connection string>"
```

Example for a local named instance with Windows authentication:
```powershell
dotnet user-secrets set "ConnectionStrings:CatalogDb" "Server=MHELAL-WINDOWS\MSSQLSERVER2022;Database=CqrsDemoLearning;Trusted_Connection=True;TrustServerCertificate=True" --project src/CqrsDemo.Api
```

Manage the secret (run from the repo root):
```powershell
dotnet user-secrets list   --project src/CqrsDemo.Api                              # view
dotnet user-secrets set    "ConnectionStrings:CatalogDb" "<new value>" --project src/CqrsDemo.Api   # modify (overwrites)
dotnet user-secrets remove "ConnectionStrings:CatalogDb" --project src/CqrsDemo.Api                 # remove one
dotnet user-secrets clear  --project src/CqrsDemo.Api                              # remove all
```
Secrets are stored in `%APPDATA%\Microsoft\UserSecrets\<UserSecretsId>\secrets.json` (the id is in `CqrsDemo.Api.csproj`) and load only when `ASPNETCORE_ENVIRONMENT=Development`. The `ConnectionStrings__CatalogDb` environment variable overrides them.

## Run it
```powershell
dotnet restore
dotnet build CqrsDemo.slnx
dotnet tool restore
dotnet ef database update --project src/CqrsDemo.Infrastructure --startup-project src/CqrsDemo.Api
dotnet run --project src/CqrsDemo.Api -- --seed-sample-data   # repeatable: second run inserts 0 rows
dotnet run --project src/CqrsDemo.Api --urls http://localhost:5199
```
Then open `src/CqrsDemo.Api/CqrsDemo.http` (VS / VS Code REST Client) and run the requests in order.

`database/migrations.sql` is the idempotent SQL script (`dotnet ef migrations script --idempotent`). Regenerate it after every new migration; run `dotnet build` between `migrations add` and any `--no-build` command.

## Tests
```powershell
dotnet test --solution CqrsDemo.slnx --filter-trait "Category=Unit"          # no database needed
$env:CQRSDEMO_TEST_SQLSERVER = "<server connection string without a database>"
dotnet test --solution CqrsDemo.slnx --filter-trait "Category=Integration"   # SQL Server required
```
Integration tests create a uniquely named `CqrsDemo_Tests_<guid>` database and drop only that database. Without the variable they are skipped.

## Endpoints
| Method | Route | Success | Errors |
|---|---|---|---|
| POST | `/api/products` | 201 + `Location` | 400 |
| GET | `/api/products/{id}` | 200 | 404 |
| GET | `/api/products?page&pageSize&name` | 200 | 400 |
| PUT | `/api/products/{id}` | 204 | 400, 404, 409 |
| POST | `/api/products/{id}/stock-adjustments` | 200 | 400, 404, 409 |
| DELETE | `/api/products/{id}` | 204 | 404 |

All errors are RFC 9457 Problem Details (`ErrorHandling/ProblemDetailsExceptionHandler.cs`).

## Documentation (`docs/`)
1. [CQRS basics and project structure](docs/01-cqrs-basics-and-structure.md)
2. [Create-product command walkthrough](docs/02-create-product-walkthrough.md)
3. [Get-products query walkthrough](docs/03-get-products-walkthrough.md)
4. [CQRS vs CRUD, Mediator, event sourcing](docs/04-comparisons.md)
5. [Debugging breakpoints](docs/05-debugging.md)
6. [Troubleshooting](docs/06-troubleshooting.md)
7. [Understanding check](docs/07-learning-questions.md)
8. [Optional extensions](docs/08-optional-extensions.md)
9. [Trade-offs, concurrency and common mistakes](docs/09-tradeoffs-and-mistakes.md)

## Suggested code reading order
`Domain/Products/Product.cs` → `Application/Products/Commands/CreateProduct/*` → `Application/Products/Queries/GetProducts/*` → `Infrastructure/Persistence/CatalogDbContext.cs` and `ProductConfiguration.cs` → `Api/Endpoints/ProductEndpoints.cs` → `Api/Program.cs` → tests.
