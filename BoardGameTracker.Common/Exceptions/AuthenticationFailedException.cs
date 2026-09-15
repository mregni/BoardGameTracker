namespace BoardGameTracker.Common.Exceptions;

public class AuthenticationFailedException : UnauthorizedAccessException
{
    public AuthenticationFailedException(string errorKey)
        : base(errorKey)
    {
    }
}
