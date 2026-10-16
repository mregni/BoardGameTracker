using BoardGameTracker.Common.Entities.Helpers;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class NightWinnerRuleTests
{
    [Fact]
    public void Decide_ShouldHaveNoWinner_WhenNobodyWon()
    {
        var result = NightWinnerRule.Decide([new NightResult(1, false), new NightResult(2, false)]);

        result.MostWins.Should().Be(0);
        result.WinnerIds.Should().BeEmpty();
    }

    [Fact]
    public void Decide_ShouldPickTheWinner_EvenAfterOneGame()
    {
        var result = NightWinnerRule.Decide([new NightResult(3, true), new NightResult(1, false)]);

        result.MostWins.Should().Be(1);
        result.WinnerIds.Should().Equal(3);
    }

    [Fact]
    public void Decide_ShouldShareTheNight_WhenPlayersWonAsOften()
    {
        var result = NightWinnerRule.Decide([
            new NightResult(4, true),
            new NightResult(2, true),
            new NightResult(4, true),
            new NightResult(2, true),
            new NightResult(9, true)
        ]);

        result.MostWins.Should().Be(2);
        result.WinnerIds.Should().Equal(2, 4);
    }
}
