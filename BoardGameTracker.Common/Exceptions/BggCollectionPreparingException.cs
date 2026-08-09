namespace BoardGameTracker.Common.Exceptions;

public class BggCollectionPreparingException : Exception
{
    public BggCollectionPreparingException()
        : base("BoardGameGeek is still preparing your collection. Please try again in a moment.")
    {
    }
}
