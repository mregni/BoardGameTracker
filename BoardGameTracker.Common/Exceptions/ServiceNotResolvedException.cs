namespace BoardGameTracker.Common.Exceptions;

public class ServiceNotResolvedException : Exception
{
    public ServiceNotResolvedException(string message): base(message)
    {
        
    }
}