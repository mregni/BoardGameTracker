namespace BoardGameTracker.Common.Exceptions;

public class ValidationException : Exception
{
    public Dictionary<string, string[]> Errors { get; }

    public IReadOnlyDictionary<string, object> Parameters { get; } = new Dictionary<string, object>();

    public ValidationException(string message)
        : base(message)
    {
        Errors = new Dictionary<string, string[]>
        {
            { "General", [message]}
        };
    }

    public ValidationException(string message, IReadOnlyDictionary<string, object> parameters)
        : this(message)
    {
        Parameters = parameters;
    }

    public ValidationException(Dictionary<string, string[]> errors)
        : base("One or more validation errors occurred.")
    {
        Errors = errors;
    }

    public ValidationException(string field, string error)
        : base(error)
    {
        Errors = new Dictionary<string, string[]>
        {
            { field, [error]}
        };
    }
}
