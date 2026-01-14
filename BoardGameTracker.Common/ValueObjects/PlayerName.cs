using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record PlayerName
{
    private const int MaxLength = 100;
    private const int MinLength = 1;

    public string Value { get; }

    public PlayerName(string value)
    {
        value = Guard.Against.NullOrWhiteSpace(value);

        if (value.Length > MaxLength)
            throw new ArgumentException($"Player name cannot exceed {MaxLength} characters.", nameof(value));

        if (value.Length < MinLength)
            throw new ArgumentException($"Player name must be at least {MinLength} character.", nameof(value));

        Value = value.Trim();
    }

    public static explicit operator string(PlayerName playerName) => playerName.Value;

    public override string ToString() => Value;
}
