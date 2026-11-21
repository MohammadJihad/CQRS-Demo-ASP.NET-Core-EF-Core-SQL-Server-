namespace CqrsDemo.Domain;

/// <summary>
/// Thrown when a requested change would break a business rule.
/// </summary>
/// <remarks>
/// The domain does not know about HTTP. The API layer translates this exception into a
/// 400 Bad Request Problem Details response (see ProblemDetailsExceptionHandler).
/// </remarks>
public sealed class DomainValidationException : Exception
{
    public DomainValidationException(IReadOnlyDictionary<string, string[]> errors)
        : base("One or more business rules were violated.")
    {
        Errors = errors;
    }

    /// <summary>Error messages grouped by the name of the field that caused them.</summary>
    public IReadOnlyDictionary<string, string[]> Errors { get; }

    public static DomainValidationException For(string field, string message) =>
        new(new Dictionary<string, string[]> { [field] = [message] });
}
