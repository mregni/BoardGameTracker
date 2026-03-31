namespace BoardGameTracker.Common.Exceptions;

public class DomainException : Exception
{
    public string ErrorCode { get; }

    public DomainException(string message)
        : base(message)
    {
        ErrorCode = "DOMAIN_ERROR";
    }

    public DomainException(string errorCode, string message)
        : base(message)
    {
        ErrorCode = errorCode;
    }

    public DomainException(string message, Exception innerException)
        : base(message, innerException)
    {
        ErrorCode = "DOMAIN_ERROR";
    }
}
