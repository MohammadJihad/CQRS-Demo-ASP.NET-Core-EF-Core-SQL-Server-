using CqrsDemo.Application.Abstractions;
using CqrsDemo.Application.Common;
using CqrsDemo.Domain.Products;
using Microsoft.EntityFrameworkCore;

namespace CqrsDemo.Application.Products.Queries.GetProducts;

/// <summary>Handles <see cref="GetProductsQuery"/>: filter, count, sort, page, project.</summary>
public sealed class GetProductsHandler(IReadDbContext db)
{
    public async Task<PagedResult<ProductSummaryDto>> HandleAsync(GetProductsQuery query, CancellationToken cancellationToken)
    {
        // Request validation (not a business rule): rejected before any SQL is sent.
        Validate(query);

        // Nothing runs yet. An IQueryable is a description of SQL that gets built up step by step.
        var products = db.Products.AsNoTracking();

        if (!string.IsNullOrWhiteSpace(query.Name))
        {
            var term = query.Name.Trim();

            // Translated to SQL "LIKE N'%term%'", with LIKE wildcards in the term escaped.
            // Case sensitivity comes from the column's collation. SQL Server's default
            // (SQL_Latin1_General_CP1_CI_AS) is case-insensitive, so "mouse" matches "Mouse".
            products = products.Where(p => p.Name.Contains(term));
        }

        // Round trip 1: SELECT COUNT(*) ... with the same filter.
        var totalCount = await products.CountAsync(cancellationToken);

        // Round trip 2: one page. A stable order is required for paging: without the Id
        // tie-breaker, products with equal names could move between pages.
        var items = await products
            .OrderBy(p => p.Name)
            .ThenBy(p => p.Id)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(p => new ProductSummaryDto(p.Id, p.Name, p.Price, p.StockQuantity, p.StockQuantity > 0))
            .ToListAsync(cancellationToken);

        return new PagedResult<ProductSummaryDto>(items, query.Page, query.PageSize, totalCount);
    }

    private static void Validate(GetProductsQuery query)
    {
        var errors = new Dictionary<string, string[]>();

        if (query.Page < 1)
        {
            errors["page"] = ["Page must be 1 or greater."];
        }
        else if ((long)(query.Page - 1) * query.PageSize > int.MaxValue)
        {
            // (Page - 1) * PageSize is the SQL OFFSET; it must fit in an int.
            errors["page"] = ["Page is too large."];
        }

        if (query.PageSize is < 1 or > GetProductsQuery.MaxPageSize)
        {
            errors["pageSize"] = [$"Page size must be between 1 and {GetProductsQuery.MaxPageSize}."];
        }

        if (query.Name?.Length > Product.NameMaxLength)
        {
            errors["name"] = [$"Name filter must be at most {Product.NameMaxLength} characters."];
        }

        if (errors.Count > 0)
        {
            throw new RequestValidationException(errors);
        }
    }
}
