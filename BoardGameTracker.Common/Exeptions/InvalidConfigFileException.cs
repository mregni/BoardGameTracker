namespace BoardGameTracker.Common.Exeptions;

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