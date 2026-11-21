using CqrsDemo.Application.Abstractions;
using CqrsDemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsDemo.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the single CatalogDbContext and exposes it through the read and write interfaces.</summary>
    /// <remarks>
    /// The connection string is read lazily (when the first DbContext is created), so the app can
    /// start and fail with a clear message only when the database is actually needed.
    /// There is deliberately no default connection string: the learning database must be
    /// configured explicitly so an existing database is never touched by accident.
    /// </remarks>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<CatalogDbContext>((serviceProvider, options) =>
        {
            var configuration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = configuration.GetConnectionString(CatalogDbContext.ConnectionStringName);

            if (string.IsNullOrWhiteSpace(connectionString))
            {
                throw new InvalidOperationException(
                    $"Connection string 'ConnectionStrings:{CatalogDbContext.ConnectionStringName}' is not configured. " +
                    "Set it with user secrets or the ConnectionStrings__CatalogDb environment variable (see README.md).");
            }

            options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure());
        });

        // Both interfaces resolve to the *same* scoped CatalogDbContext instance per HTTP request.
        services.AddScoped<IWriteDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());
        services.AddScoped<IReadDbContext>(sp => sp.GetRequiredService<CatalogDbContext>());

        return services;
    }
}
