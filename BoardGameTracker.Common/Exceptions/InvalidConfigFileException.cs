namespace BoardGameTracker.Common.Exceptions;

public class InvalidConfigFileException : Exception
{
    public InvalidConfigFileException(string message)
        : base(message)
    {
    }
    
    public InvalidConfigFileException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}