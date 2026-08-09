namespace BoardGameTracker.Common.Exceptions;

public class BggRateLimitException : Exception
{
    public BggRateLimitException()
        : base("BoardGameGeek is rate-limiting requests. Please wait a moment and try again.")
    { }
}
