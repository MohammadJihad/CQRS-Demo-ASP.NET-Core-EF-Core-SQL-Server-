using CqrsDemo.Domain;
using CqrsDemo.Domain.Products;

namespace CqrsDemo.Tests.Unit;

[Trait("Category", "Unit")]
public class ProductTests
{
    private static readonly DateTime Now = new(2026, 1, 1, 12, 0, 0, DateTimeKind.Utc);

    private static Product NewProduct(int stock = 5) => Product.Create("Keyboard", "Clicky", 10.50m, stock, Now);

    private static IReadOnlyDictionary<string, string[]> ErrorsOf(Action act) =>
        Assert.Throws<DomainValidationException>(act).Errors;

    [Fact]
    public void Create_with_valid_values_sets_every_field()
    {
        var product = Product.Create("  Keyboard  ", "Clicky", 10.50m, 5, Now);

        Assert.NotEqual(Guid.Empty, product.Id);
        Assert.Equal("Keyboard", product.Name);
        Assert.Equal("Clicky", product.Description);
        Assert.Equal(10.50m, product.Price);
        Assert.Equal(5, product.StockQuantity);
        Assert.Equal(Now, product.CreatedAtUtc);
    }

    [Fact]
    public void Create_allows_zero_price_zero_stock_and_no_description()
    {
        var product = Product.Create("Freebie", null, 0m, 0, Now);

        Assert.Null(product.Description);
        Assert.Equal(0m, product.Price);
        Assert.Equal(0, product.StockQuantity);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Create_rejects_missing_name(string? name) =>
        Assert.Contains("Name", ErrorsOf(() => Product.Create(name, null, 1m, 1, Now)).Keys);

    [Fact]
    public void Create_rejects_negative_price() =>
        Assert.Contains("Price", ErrorsOf(() => Product.Create("A", null, -0.01m, 1, Now)).Keys);

    [Fact]
    public void Create_rejects_more_than_two_decimal_places() =>
        Assert.Contains("Price", ErrorsOf(() => Product.Create("A", null, 1.999m, 1, Now)).Keys);

    [Fact]
    public void Create_rejects_negative_stock() =>
        Assert.Contains("StockQuantity", ErrorsOf(() => Product.Create("A", null, 1m, -1, Now)).Keys);

    [Fact]
    public void Create_reports_all_violations_together()
    {
        var errors = ErrorsOf(() => Product.Create("", null, -1m, -1, Now));

        Assert.Equal(["Name", "Price", "StockQuantity"], errors.Keys.Order());
    }

    [Fact]
    public void Create_rejects_name_longer_than_column()
    {
        var name = new string('x', Product.NameMaxLength + 1);

        Assert.Contains("Name", ErrorsOf(() => Product.Create(name, null, 1m, 1, Now)).Keys);
    }

    [Fact]
    public void UpdateDetails_changes_details_but_not_stock()
    {
        var product = NewProduct(stock: 7);

        product.UpdateDetails("Renamed", null, 12m);

        Assert.Equal("Renamed", product.Name);
        Assert.Null(product.Description);
        Assert.Equal(12m, product.Price);
        Assert.Equal(7, product.StockQuantity);
    }

    [Fact]
    public void UpdateDetails_with_invalid_values_leaves_product_unchanged()
    {
        var product = NewProduct();

        ErrorsOf(() => product.UpdateDetails("", "x", -5m));

        Assert.Equal("Keyboard", product.Name);
        Assert.Equal(10.50m, product.Price);
    }

    [Theory]
    [InlineData(3, 8)]
    [InlineData(-5, 0)]
    public void AdjustStock_applies_valid_changes(int change, int expected)
    {
        var product = NewProduct(stock: 5);

        product.AdjustStock(change);

        Assert.Equal(expected, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_that_would_go_negative_is_rejected_and_stock_is_unchanged()
    {
        var product = NewProduct(stock: 5);

        Assert.Contains("QuantityChange", ErrorsOf(() => product.AdjustStock(-6)).Keys);
        Assert.Equal(5, product.StockQuantity);
    }

    [Fact]
    public void AdjustStock_rejects_zero_change() =>
        Assert.Contains("QuantityChange", ErrorsOf(() => NewProduct().AdjustStock(0)).Keys);

    [Fact]
    public void AdjustStock_rejects_overflow()
    {
        var product = NewProduct(stock: int.MaxValue);

        Assert.Contains("QuantityChange", ErrorsOf(() => product.AdjustStock(1)).Keys);
    }
}
