namespace BoardGameTracker.Common.Exceptions;

public class BggFeatureDisabledException : Exception
{
    public BggFeatureDisabledException()
        : base("BGG features are disabled. Please configure a BGG API key in settings.")
    { }
}
