namespace CqrsDemo.Application.Common;

/// <summary>
/// Thrown when a request is malformed for reasons that are <i>not</i> business rules, for
/// example a page number of 0 or a missing required field. The API translates it into 400 Bad Request.
/// </summary>
/// <remarks>
/// Business rules ("price cannot be negative") live in the Domain and raise DomainValidationException.
/// Keeping the two apart shows where each kind of rule belongs. Both produce the same
/// Problem Details shape, so callers don't need to care which layer rejected the request.
/// </remarks>
public sealed class RequestValidationException(IReadOnlyDictionary<string, string[]> errors)
    : Exception("The request is invalid.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
