using Microsoft.Extensions.Configuration;

namespace BoardGameTracker.Core.Common;

public class DateTimeProvider : IDateTimeProvider
{
    private readonly TimeZoneInfo _timeZone;

    public DateTimeProvider(IConfiguration configuration)
    {
        var timeZoneId = configuration["TZ"]
                         ?? Environment.GetEnvironmentVariable("TZ")
                         ?? "UTC";

        try
        {
            _timeZone = TimeZoneInfo.FindSystemTimeZoneById(timeZoneId);
        }
        catch (TimeZoneNotFoundException)
        {
            Console.WriteLine($"Warning: Timezone '{timeZoneId}' not found. Falling back to UTC.");
            _timeZone = TimeZoneInfo.Utc;
        }
    }

    public DateTime UtcNow => DateTime.UtcNow;

    public DateTime Now => TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, _timeZone);

    public TimeZoneInfo TimeZone => _timeZone;

    public DateTime ConvertToLocalTime(DateTime utcDateTime)
    {
        if (utcDateTime.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("DateTime must be in UTC", nameof(utcDateTime));
        }

        return TimeZoneInfo.ConvertTimeFromUtc(utcDateTime, _timeZone);
    }

    public DateTime ConvertToUtc(DateTime localDateTime)
    {
        if (localDateTime.Kind == DateTimeKind.Utc)
        {
            return localDateTime;
        }

        return TimeZoneInfo.ConvertTimeToUtc(localDateTime, _timeZone);
    }
}
