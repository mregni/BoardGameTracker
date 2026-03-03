using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record PlayTimeRange
{
    public int MinMinutes { get; }
    public int MaxMinutes { get; }

    public PlayTimeRange(int minMinutes, int maxMinutes)
    {
        Guard.Against.Negative(minMinutes);
        Guard.Against.Negative(maxMinutes);

        if (maxMinutes < minMinutes)
        {
            throw new ArgumentException(
                $"Maximum play time ({maxMinutes}) cannot be less than minimum play time ({minMinutes}).");
        }

        MinMinutes = minMinutes;
        MaxMinutes = maxMinutes;
    }
}
