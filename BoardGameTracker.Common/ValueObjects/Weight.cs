using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record Weight
{
    public double Value { get; }

    private const double MinWeight = 0;
    private const double MaxWeight = 5;

    public Weight(double value)
    {
        Guard.Against.OutOfRange(value, nameof(value), MinWeight, MaxWeight);

        // Round to 2 decimal places to avoid floating point precision issues
        Value = Math.Round(value, 2);
    }

    public static implicit operator double(Weight weight) => weight.Value;

    public override string ToString() => Value.ToString("F2");
}
