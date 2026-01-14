using Ardalis.GuardClauses;

namespace BoardGameTracker.Common.ValueObjects;

public record Price
{
    public decimal Amount { get; }

    public Price(decimal amount)
    {
        Guard.Against.Negative(amount);

        // Round to 2 decimal places for currency
        Amount = Math.Round(amount, 2);
    }

    public static implicit operator decimal(Price price) => price.Amount;

    public override string ToString() => Amount.ToString("C");
}
