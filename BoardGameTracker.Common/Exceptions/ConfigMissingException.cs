namespace BoardGameTracker.Common.Exceptions;

public class ConfigMissingException : Exception
{
    public string ConfigKey { get; }

    public ConfigMissingException(string configKey)
        : base($"Configuration key '{configKey}' was not found in the database and no environment variable override exists.")
    {
        ConfigKey = configKey;
    }
}
