using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using CqrsDemo.Application.Products.Commands.AdjustProductStock;
using CqrsDemo.Domain.Products;
using CqrsDemo.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CqrsDemo.Tests.Integration;

/// <summary>
/// End-to-end tests: real HTTP pipeline, real handlers, real SQL Server (isolated test database).
/// Each test uses a unique name prefix so tests never depend on each other's rows.
/// </summary>
[Collection(SqlServerCollection.Name)]
[Trait("Category", "Integration")]
public class ProductApiTests(SqlServerFixture fixture)
{
    private readonly HttpClient _client = fixture.CreateClient();

    private static string Unique() => $"T{Guid.NewGuid():N}";

    private async Task<Guid> CreateAsync(string name, decimal price = 10m, int stock = 5)
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name, description = "d", price, stockQuantity = stock });
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
    }

    private static async Task<JsonElement> ProblemAsync(HttpResponseMessage response)
    {
        Assert.Equal("application/problem+json", response.Content.Headers.ContentType?.MediaType);
        return await response.Content.ReadFromJsonAsync<JsonElement>();
    }

    [SqlServerFact]
    public async Task Create_returns_201_with_location_and_the_product_can_be_read_back()
    {
        var name = Unique();
        var response = await _client.PostAsJsonAsync("/api/products", new { name, description = "desc", price = 12.34m, stockQuantity = 3 });

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var id = (await response.Content.ReadFromJsonAsync<JsonElement>()).GetProperty("id").GetGuid();
        Assert.EndsWith($"/api/products/{id}", response.Headers.Location!.ToString());

        var get = await _client.GetAsync(response.Headers.Location);
        Assert.Equal(HttpStatusCode.OK, get.StatusCode);
        var body = await get.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(name, body.GetProperty("name").GetString());
        Assert.Equal(12.34m, body.GetProperty("price").GetDecimal());
        Assert.Equal(3, body.GetProperty("stockQuantity").GetInt32());
        Assert.True(body.GetProperty("isInStock").GetBoolean());
        Assert.EndsWith("Z", body.GetProperty("createdAtUtc").GetString());
    }

    [SqlServerFact]
    public async Task Create_with_invalid_input_returns_400_problem_details_listing_every_error()
    {
        var response = await _client.PostAsJsonAsync("/api/products", new { name = "", price = -1m, stockQuantity = -1 });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        var errors = (await ProblemAsync(response)).GetProperty("errors");
        Assert.True(errors.TryGetProperty("Name", out _));
        Assert.True(errors.TryGetProperty("Price", out _));
        Assert.True(errors.TryGetProperty("StockQuantity", out _));
    }

    [SqlServerFact]
    public async Task Malformed_json_returns_400_problem_details()
    {
        var response = await _client.PostAsync("/api/products", new StringContent("{ not json", System.Text.Encoding.UTF8, "application/json"));

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        await ProblemAsync(response);
    }

    [SqlServerFact]
    public async Task Missing_product_returns_404_for_every_operation()
    {
        var id = Guid.NewGuid();

        var responses = new[]
        {
            await _client.GetAsync($"/api/products/{id}"),
            await _client.PutAsJsonAsync($"/api/products/{id}", new { name = "x", price = 1m }),
            await _client.PostAsJsonAsync($"/api/products/{id}/stock-adjustments", new { quantityChange = 1 }),
            await _client.DeleteAsync($"/api/products/{id}"),
        };

        foreach (var response in responses)
        {
            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
            await ProblemAsync(response);
        }
    }

    [SqlServerFact]
    public async Task Update_changes_price_and_returns_204()
    {
        var id = await CreateAsync(Unique(), price: 10m, stock: 4);

        var put = await _client.PutAsJsonAsync($"/api/products/{id}", new { name = "Renamed " + Unique(), description = (string?)null, price = 99.99m });

        Assert.Equal(HttpStatusCode.NoContent, put.StatusCode);
        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
        Assert.Equal(99.99m, body.GetProperty("price").GetDecimal());
        Assert.Equal(4, body.GetProperty("stockQuantity").GetInt32());
    }

    [SqlServerFact]
    public async Task Update_with_negative_price_returns_400_and_changes_nothing()
    {
        var id = await CreateAsync(Unique(), price: 10m);

        var put = await _client.PutAsJsonAsync($"/api/products/{id}", new { name = "x", price = -5m });

        Assert.Equal(HttpStatusCode.BadRequest, put.StatusCode);
        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
        Assert.Equal(10m, body.GetProperty("price").GetDecimal());
    }

    [SqlServerFact]
    public async Task Adjust_stock_returns_before_and_after_and_rejects_negative_result()
    {
        var id = await CreateAsync(Unique(), stock: 5);

        var ok = await _client.PostAsJsonAsync($"/api/products/{id}/stock-adjustments", new { quantityChange = -3 });
        Assert.Equal(HttpStatusCode.OK, ok.StatusCode);
        var result = await ok.Content.ReadFromJsonAsync<StockAdjustmentResult>();
        Assert.Equal(new StockAdjustmentResult(id, 5, 2), result);

        var tooMuch = await _client.PostAsJsonAsync($"/api/products/{id}/stock-adjustments", new { quantityChange = -3 });
        Assert.Equal(HttpStatusCode.BadRequest, tooMuch.StatusCode);
        Assert.True((await ProblemAsync(tooMuch)).GetProperty("errors").TryGetProperty("QuantityChange", out _));

        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
        Assert.Equal(2, body.GetProperty("stockQuantity").GetInt32());
    }

    [SqlServerFact]
    public async Task Delete_returns_204_then_get_returns_404()
    {
        var id = await CreateAsync(Unique());

        Assert.Equal(HttpStatusCode.NoContent, (await _client.DeleteAsync($"/api/products/{id}")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await _client.GetAsync($"/api/products/{id}")).StatusCode);
    }

    [SqlServerFact]
    public async Task List_filters_by_name_pages_and_orders_deterministically()
    {
        var prefix = Unique();
        // Names sort alphabetically: A..E. Two products share a name to exercise the Id tie-breaker.
        foreach (var suffix in new[] { "C", "A", "E", "B", "B" })
        {
            await CreateAsync($"{prefix}-{suffix}");
        }

        await CreateAsync(Unique());

        var page1 = await _client.GetFromJsonAsync<JsonElement>($"/api/products?name={prefix}&page=1&pageSize=2");
        var page2 = await _client.GetFromJsonAsync<JsonElement>($"/api/products?name={prefix}&page=2&pageSize=2");
        var page3 = await _client.GetFromJsonAsync<JsonElement>($"/api/products?name={prefix}&page=3&pageSize=2");

        Assert.Equal(5, page1.GetProperty("totalCount").GetInt32());
        Assert.Equal(3, page1.GetProperty("totalPages").GetInt32());

        var names = new[] { page1, page2, page3 }
            .SelectMany(p => p.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("name").GetString()!))
            .ToArray();
        Assert.Equal(new[] { "A", "B", "B", "C", "E" }.Select(s => $"{prefix}-{s}"), names);

        var ids = new[] { page1, page2, page3 }
            .SelectMany(p => p.GetProperty("items").EnumerateArray().Select(i => i.GetProperty("id").GetGuid()))
            .ToArray();
        Assert.Equal(ids.Length, ids.Distinct().Count());
    }

    [SqlServerFact]
    public async Task List_name_filter_matches_case_insensitively_and_treats_wildcards_literally()
    {
        var prefix = Unique();
        await CreateAsync($"{prefix}-Wireless Mouse");
        await CreateAsync($"{prefix}-100% Cotton");

        var mouse = await _client.GetFromJsonAsync<JsonElement>($"/api/products?name={prefix}-wireless");
        Assert.Equal(1, mouse.GetProperty("totalCount").GetInt32());

        var percent = await _client.GetFromJsonAsync<JsonElement>($"/api/products?name={Uri.EscapeDataString(prefix + "-%")}");
        Assert.Equal(0, percent.GetProperty("totalCount").GetInt32());
    }

    [SqlServerTheory]
    [InlineData("page=0")]
    [InlineData("pageSize=0")]
    [InlineData("pageSize=101")]
    [InlineData("page=abc")]
    public async Task List_rejects_invalid_paging_with_400(string query)
    {
        var response = await _client.GetAsync($"/api/products?{query}");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [SqlServerFact]
    public async Task Reads_do_not_modify_stored_data()
    {
        var id = await CreateAsync(Unique());

        var before = await SnapshotAsync();
        await _client.GetAsync($"/api/products/{id}");
        await _client.GetAsync("/api/products?page=1&pageSize=100");
        await _client.GetAsync($"/api/products/{Guid.NewGuid()}");
        var after = await SnapshotAsync();

        // The snapshot includes each row's rowversion, which SQL Server changes on any write.
        Assert.Equal(before, after);
    }

    private async Task<string> SnapshotAsync()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var rows = await db.Products.AsNoTracking().OrderBy(p => p.Id)
            .Select(p => new { p.Id, p.Name, p.Price, p.StockQuantity, RowVersion = EF.Property<byte[]>(p, "RowVersion") })
            .ToListAsync();
        return string.Join("|", rows.Select(r => $"{r.Id}:{r.Name}:{r.Price}:{r.StockQuantity}:{Convert.ToHexString(r.RowVersion)}"));
    }

    [SqlServerFact]
    public async Task Concurrent_stock_adjustments_never_oversell()
    {
        var id = await CreateAsync(Unique(), stock: 1);

        var responses = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
            _client.PostAsJsonAsync($"/api/products/{id}/stock-adjustments", new { quantityChange = -1 })));

        // One request wins. Losers get 409 (they read the same row version) or 400 (they read
        // after the winner saved). Nothing may succeed twice or fail with a server error.
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.OK));
        Assert.All(responses, r => Assert.Contains(r.StatusCode, new[] { HttpStatusCode.OK, HttpStatusCode.Conflict, HttpStatusCode.BadRequest }));

        var body = await _client.GetFromJsonAsync<JsonElement>($"/api/products/{id}");
        Assert.Equal(0, body.GetProperty("stockQuantity").GetInt32());
    }

    [SqlServerFact]
    public async Task Stale_write_is_rejected_by_rowversion_check()
    {
        var id = await CreateAsync(Unique(), stock: 5);

        using var scopeA = fixture.CreateScope();
        using var scopeB = fixture.CreateScope();
        var dbA = scopeA.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var dbB = scopeB.ServiceProvider.GetRequiredService<CatalogDbContext>();
        var a = await dbA.Products.SingleAsync(p => p.Id == id);
        var b = await dbB.Products.SingleAsync(p => p.Id == id);

        a.AdjustStock(-4);
        await dbA.SaveChangesAsync();

        b.AdjustStock(-4);
        await Assert.ThrowsAsync<DbUpdateConcurrencyException>(() => dbB.SaveChangesAsync());
    }

    [SqlServerFact]
    public async Task Seeding_twice_does_not_duplicate_sample_products()
    {
        using var scope = fixture.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<CatalogDbContext>();

        var first = await SampleDataSeeder.SeedAsync(db, TimeProvider.System);
        var second = await SampleDataSeeder.SeedAsync(db, TimeProvider.System);

        Assert.Equal(12, first);
        Assert.Equal(0, second);
        Assert.Equal(1, await db.Products.CountAsync(p => p.Name == "Cable Organizer"));
    }
}

public sealed class SqlServerTheoryAttribute : TheoryAttribute
{
    public SqlServerTheoryAttribute([System.Runtime.CompilerServices.CallerFilePath] string? sourceFilePath = null, [System.Runtime.CompilerServices.CallerLineNumber] int sourceLineNumber = -1)
        : base(sourceFilePath, sourceLineNumber)
    {
        if (!SqlServerFixture.IsConfigured)
        {
            Skip = $"Set {SqlServerFixture.EnvironmentVariable} to run SQL Server integration tests.";
        }
    }
}
