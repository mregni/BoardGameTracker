namespace BoardGameTracker.Common.Exceptions;

public class ForbiddenException : Exception
{
    public ForbiddenException(string message)
        : base(message)
    {
    }
}
