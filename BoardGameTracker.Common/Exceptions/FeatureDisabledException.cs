namespace BoardGameTracker.Common.Exceptions;

public class FeatureDisabledException : Exception
{
    public string Feature { get; }

    public FeatureDisabledException(string feature)
        : base(Constants.Errors.FeatureDisabled)
    {
        Feature = feature;
    }
}
