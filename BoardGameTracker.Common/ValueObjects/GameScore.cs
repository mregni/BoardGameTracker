using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record GameScore
{
    public double Value { get; }

    public GameScore(double value)
    {
        Guard.Against.Negative(value);
        Value = value;
    }

    public static implicit operator double(GameScore score) => score.Value;

    public static GameScore operator +(GameScore a, GameScore b) => new(a.Value + b.Value);

    public static GameScore operator -(GameScore a, GameScore b) => new(a.Value - b.Value);
}
