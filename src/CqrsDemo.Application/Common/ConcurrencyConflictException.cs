namespace CqrsDemo.Application.Common;

/// <summary>
/// Thrown when another request changed the same row between the moment a command loaded it and
/// the moment it tried to save. The API translates it into 409 Conflict, and the caller should retry.
/// </summary>
/// <remarks>
/// Detection relies on the SQL Server rowversion column configured in ProductConfiguration
/// (optimistic concurrency). See docs/09-tradeoffs-and-mistakes.md for the full explanation.
/// </remarks>
public sealed class ConcurrencyConflictException(string resourceName, object key, Exception innerException)
    : Exception($"{resourceName} '{key}' was modified by another request. Reload it and try again.", innerException);
