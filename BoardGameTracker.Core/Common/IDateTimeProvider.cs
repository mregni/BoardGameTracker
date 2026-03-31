namespace BoardGameTracker.Core.Common;

/// <summary>
/// Provides access to current date/time with timezone awareness.
/// Useful for testing and handling different timezones in Docker environments.
/// </summary>
public interface IDateTimeProvider
{
    /// <summary>
    /// Gets the current UTC time.
    /// </summary>
    DateTime UtcNow { get; }

    /// <summary>
    /// Gets the current time in the configured timezone.
    /// </summary>
    DateTime Now { get; }

    /// <summary>
    /// Gets the configured timezone info.
    /// </summary>
    TimeZoneInfo TimeZone { get; }

    /// <summary>
    /// Converts a UTC datetime to the configured timezone.
    /// </summary>
    DateTime ConvertToLocalTime(DateTime utcDateTime);

    /// <summary>
    /// Converts a local datetime to UTC.
    /// </summary>
    DateTime ConvertToUtc(DateTime localDateTime);
}
