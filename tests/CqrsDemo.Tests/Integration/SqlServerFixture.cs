using CqrsDemo.Infrastructure.Persistence;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsDemo.Tests.Integration;

/// <summary>
/// Creates one brand-new, uniquely named database for the whole integration run, applies the real
/// EF migrations to it, and drops it afterwards. It never touches any other database.
/// </summary>
/// <remarks>
/// Set <see cref="EnvironmentVariable"/> to a connection string for a SQL Server instance (the
/// Database part is ignored and replaced). Without it, integration tests are skipped.
/// </remarks>
public sealed class SqlServerFixture : IAsyncLifetime
{
    public const string EnvironmentVariable = "CQRSDEMO_TEST_SQLSERVER";

    private WebApplicationFactory<Program>? _factory;
    private string? _connectionString;

    public static bool IsConfigured => !string.IsNullOrWhiteSpace(Environment.GetEnvironmentVariable(EnvironmentVariable));

    public HttpClient CreateClient() => _factory!.CreateClient();

    public IServiceScope CreateScope() => _factory!.Services.CreateScope();

    public async ValueTask InitializeAsync()
    {
        if (!IsConfigured)
        {
            return;
        }

        var builder = new SqlConnectionStringBuilder(Environment.GetEnvironmentVariable(EnvironmentVariable))
        {
            InitialCatalog = $"CqrsDemo_Tests_{Guid.NewGuid():N}",
        };
        _connectionString = builder.ConnectionString;

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(web =>
            web.UseSetting($"ConnectionStrings:{CatalogDbContext.ConnectionStringName}", _connectionString));

        // Applying the migrations here means every integration test also proves they work.
        using var scope = _factory.Services.CreateScope();
        await scope.ServiceProvider.GetRequiredService<CatalogDbContext>().Database.MigrateAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_factory is null)
        {
            return;
        }

        using (var scope = _factory.Services.CreateScope())
        {
            var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
            var name = new SqlConnectionStringBuilder(_connectionString).InitialCatalog;

            // Safety net: only ever delete a database this fixture named itself.
            if (name.StartsWith("CqrsDemo_Tests_", StringComparison.Ordinal))
            {
                await db.Database.EnsureDeletedAsync();
            }
        }

        await _factory.DisposeAsync();
    }
}

[CollectionDefinition(Name)]
public sealed class SqlServerCollection : ICollectionFixture<SqlServerFixture>
{
    public const string Name = "SqlServer";
}

/// <summary>A [Fact] that is skipped, with a clear reason, when no SQL Server is configured.</summary>
public sealed class SqlServerFactAttribute : FactAttribute
{
    public SqlServerFactAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!SqlServerFixture.IsConfigured)
        {
            Skip = $"Set {SqlServerFixture.EnvironmentVariable} to run SQL Server integration tests.";
        }
    }
}
