namespace BoardGameTracker.Common.Exeptions;

public class ServiceNotResolvedException : Exception
{
    public ServiceNotResolvedException(string message): base(message)
    {
        
    }
}