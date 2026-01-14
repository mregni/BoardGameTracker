using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record Rating
{
    public double Value { get; }

    private const double MinRating = 0;
    private const double MaxRating = 10;

    public Rating(double value)
    {
        Guard.Against.OutOfRange(value, nameof(value), MinRating, MaxRating);

        // Round to 2 decimal places to avoid floating point precision issues
        Value = Math.Round(value, 2);
    }

    public static implicit operator double(Rating rating) => rating.Value;

    public override string ToString() => Value.ToString("F2");
}
