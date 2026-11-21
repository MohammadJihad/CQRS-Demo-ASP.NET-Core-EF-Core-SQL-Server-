namespace CqrsDemo.Domain.Products;

/// <summary>
/// The write model for a catalog product.
/// </summary>
/// <remarks>
/// In CQRS the write side cares about one thing: only valid state changes may happen.
/// That is why every property has a private setter and every change goes through a method
/// that checks the business rules first. Command handlers load this entity, ask it to change,
/// and save it. Query handlers never return this type to callers; they project the same table
/// into read-only DTOs instead (see CqrsDemo.Application/Products/Queries).
/// </remarks>
public sealed class Product
{
    // Shared with the EF Core configuration so the database column sizes and the business rules
    // can never disagree (otherwise SQL Server would reject the row with a truncation error).
    public const int NameMaxLength = 200;
    public const int DescriptionMaxLength = 2000;

    public Guid Id { get; private set; }

    public string Name { get; private set; }

    public string? Description { get; private set; }

    public decimal Price { get; private set; }

    public int StockQuantity { get; private set; }

    public DateTime CreatedAtUtc { get; private set; }

    // Used only by EF Core when it materializes a row. Application code must call Create(...),
    // which is the single place where a new product can come into existence.
    private Product()
    {
        Name = string.Empty;
    }

    /// <summary>Creates a new product with a newly generated identifier.</summary>
    /// <exception cref="DomainValidationException">A business rule is violated.</exception>
    public static Product Create(
        string? name,
        string? description,
        decimal price,
        int stockQuantity,
        DateTime createdAtUtc) =>
        Create(Guid.NewGuid(), name, description, price, stockQuantity, createdAtUtc);

    /// <summary>
    /// Creates a new product with a caller-chosen identifier. The sample-data seeder uses this
    /// so that running it twice inserts nothing new.
    /// </summary>
    /// <remarks>
    /// The entity owns its identity, so the id exists before the database is touched and the
    /// create command can return it immediately. Random GUIDs fragment a clustered index on very
    /// large tables; sequential GUIDs are the usual fix, but they are not needed for this sample.
    /// </remarks>
    /// <exception cref="DomainValidationException">A business rule is violated.</exception>
    public static Product Create(
        Guid id,
        string? name,
        string? description,
        decimal price,
        int stockQuantity,
        DateTime createdAtUtc)
    {
        if (id == Guid.Empty)
        {
            throw new ArgumentException("A product id must not be empty.", nameof(id));
        }

        // A non-UTC timestamp is a programming error, not invalid user input, so it is not
        // reported as a validation failure.
        if (createdAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("CreatedAtUtc must be a UTC DateTime.", nameof(createdAtUtc));
        }

        var errors = new ValidationErrors();
        var validName = ValidateName(name, errors);
        var validDescription = ValidateDescription(description, errors);
        ValidatePrice(price, errors);

        if (stockQuantity < 0)
        {
            errors.Add(nameof(StockQuantity), "Stock quantity cannot be negative.");
        }

        // All rules are checked before anything is assigned, and all violations are reported
        // together, so the caller can fix every problem in one round trip.
        errors.ThrowIfAny();

        return new Product
        {
            Id = id,
            Name = validName,
            Description = validDescription,
            Price = price,
            StockQuantity = stockQuantity,
            CreatedAtUtc = createdAtUtc,
        };
    }

    /// <summary>
    /// Replaces the descriptive details of the product.
    /// </summary>
    /// <remarks>
    /// Stock is deliberately not part of this method. Changing stock is a different business
    /// intent with its own rule (the result can't go below zero), so it has its own command:
    /// AdjustProductStock. Giving each intent its own command is a core CQRS habit.
    /// </remarks>
    /// <exception cref="DomainValidationException">A business rule is violated; the product is left unchanged.</exception>
    public void UpdateDetails(string? name, string? description, decimal price)
    {
        var errors = new ValidationErrors();
        var validName = ValidateName(name, errors);
        var validDescription = ValidateDescription(description, errors);
        ValidatePrice(price, errors);
        errors.ThrowIfAny();

        Name = validName;
        Description = validDescription;
        Price = price;
    }

    /// <summary>
    /// Increases (positive value) or decreases (negative value) the quantity in stock.
    /// </summary>
    /// <exception cref="DomainValidationException">The change is zero or would make stock negative; the product is left unchanged.</exception>
    public void AdjustStock(int quantityChange)
    {
        if (quantityChange == 0)
        {
            throw DomainValidationException.For("QuantityChange", "Quantity change must not be zero.");
        }

        // long arithmetic avoids silent int overflow for extreme inputs.
        var newQuantity = (long)StockQuantity + quantityChange;

        if (newQuantity < 0)
        {
            throw DomainValidationException.For(
                "QuantityChange",
                $"Stock quantity cannot be negative. Current stock is {StockQuantity}; a change of {quantityChange} would result in {newQuantity}.");
        }

        if (newQuantity > int.MaxValue)
        {
            throw DomainValidationException.For("QuantityChange", "Stock quantity would exceed the maximum supported value.");
        }

        StockQuantity = (int)newQuantity;
    }

    private static string ValidateName(string? name, ValidationErrors errors)
    {
        var trimmed = name?.Trim() ?? string.Empty;

        if (trimmed.Length == 0)
        {
            errors.Add(nameof(Name), "Product name is required.");
        }
        else if (trimmed.Length > NameMaxLength)
        {
            errors.Add(nameof(Name), $"Product name must be at most {NameMaxLength} characters.");
        }

        return trimmed;
    }

    private static string? ValidateDescription(string? description, ValidationErrors errors)
    {
        var trimmed = description?.Trim();

        if (trimmed?.Length > DescriptionMaxLength)
        {
            errors.Add(nameof(Description), $"Description must be at most {DescriptionMaxLength} characters.");
        }

        return string.IsNullOrEmpty(trimmed) ? null : trimmed;
    }

    private static void ValidatePrice(decimal price, ValidationErrors errors)
    {
        if (price < 0)
        {
            errors.Add(nameof(Price), "Price cannot be negative.");
        }

        // The column is decimal(18,2). Without this rule SQL Server would silently round
        // 9.999 to 10.00, so the stored price would differ from what the caller sent.
        if (decimal.Round(price, 2) != price)
        {
            errors.Add(nameof(Price), "Price can have at most 2 decimal places.");
        }
    }
}
