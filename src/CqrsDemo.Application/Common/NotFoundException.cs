namespace CqrsDemo.Application.Common;

/// <summary>
/// Thrown by a command handler when the entity the command targets doesn't exist.
/// The API translates it into 404 Not Found.
/// </summary>
/// <remarks>
/// A command can't do its job without its target, so this is a failure. A query asking for a
/// missing product simply returns null instead; "no such product" is a valid answer to a question.
/// </remarks>
public sealed class NotFoundException(string resourceName, object key)
    : Exception($"{resourceName} '{key}' was not found.")
{
    public string ResourceName { get; } = resourceName;

    public object Key { get; } = key;
}
