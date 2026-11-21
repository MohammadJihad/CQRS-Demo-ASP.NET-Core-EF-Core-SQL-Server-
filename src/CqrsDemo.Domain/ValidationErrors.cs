namespace CqrsDemo.Domain;

/// <summary>Collects every rule violation so they can be reported together.</summary>
internal sealed class ValidationErrors
{
    private readonly Dictionary<string, List<string>> _errors = new();

    public void Add(string field, string message)
    {
        if (!_errors.TryGetValue(field, out var messages))
        {
            messages = [];
            _errors[field] = messages;
        }

        messages.Add(message);
    }

    public void ThrowIfAny()
    {
        if (_errors.Count > 0)
        {
            throw new DomainValidationException(_errors.ToDictionary(e => e.Key, e => e.Value.ToArray()));
        }
    }
}
