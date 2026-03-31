using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record PlayerCountRange
{
    public int Min { get; }
    public int Max { get; }

    public PlayerCountRange(int min, int max)
    {
        Guard.Against.NegativeOrZero(min);
        Guard.Against.NegativeOrZero(max);

        if (max < min)
            throw new ArgumentException("Maximum players cannot be less than minimum players.");

        Min = min;
        Max = max;
    }
}
