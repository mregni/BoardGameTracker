using BoardGameTracker.Common.ValueObjects;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.ValueObjects;

public class PriceTests
{
    [Theory]
    [InlineData("0", "0")]
    [InlineData("12.5", "12.5")]
    [InlineData("12.344", "12.34")]
    [InlineData("12.346", "12.35")]
    public void Constructor_ShouldKeepTwoDecimals(string amount, string expected)
    {
        new Price(decimal.Parse(amount, System.Globalization.CultureInfo.InvariantCulture)).Amount
            .Should().Be(decimal.Parse(expected, System.Globalization.CultureInfo.InvariantCulture));
    }

    [Fact]
    public void Constructor_ShouldRefuseANegativeAmount()
    {
        var act = () => new Price(-0.01m);

        act.Should().Throw<ArgumentException>().WithParameterName("amount");
    }

    [Fact]
    public void ImplicitConversion_ShouldReturnTheAmount()
    {
        decimal amount = new Price(12.5m);

        amount.Should().Be(12.5m);
    }

    [Fact]
    public void Equality_ShouldCompareTheRoundedAmount()
    {
        new Price(1.001m).Should().Be(new Price(1.00m));
        new Price(1.01m).Should().NotBe(new Price(1.00m));
    }

    [Fact]
    public void ToString_ShouldUseTheInvariantCurrencyFormat()
    {
        new Price(1234.5m).ToString().Should().Be("\u00a41,234.50");
    }
}
