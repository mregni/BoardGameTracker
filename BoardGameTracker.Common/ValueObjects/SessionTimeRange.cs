using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record SessionTimeRange
{
    public DateTime Start { get; }
    public DateTime End { get; }

    public SessionTimeRange(DateTime start, DateTime end)
    {
        Guard.Against.Default(start);
        Guard.Against.Default(end);

        if (end < start)
            throw new ArgumentException("Session end time cannot be before start time.", nameof(end));

        Start = start;
        End = end;
    }

    public TimeSpan Duration => End - Start;
}
