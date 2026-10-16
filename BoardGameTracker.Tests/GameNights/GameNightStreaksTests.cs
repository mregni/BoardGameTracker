using System.Linq;
using BoardGameTracker.Core.GameNights;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class GameNightStreaksTests
{
    private static readonly DateTime Day = new(2026, 1, 1, 20, 0, 0, DateTimeKind.Utc);

    [Fact]
    public void Find_ShouldReturnNothing_WhenThereAreNoNights()
    {
        var result = GameNightStreaks.Find([]);

        result.Runs.Should().BeEmpty();
        result.LengthPerStep.Should().BeEmpty();
    }

    [Fact]
    public void Find_ShouldCloseARun_WhenTheKeyIsMissing_AndKeepTheLastRunsOpen()
    {
        var result = GameNightStreaks.Find([
            new StreakStep(Day, [1, 2]),
            new StreakStep(Day.AddDays(1), [1, 1]),
            new StreakStep(Day.AddDays(2), [2]),
            new StreakStep(Day.AddDays(3), []),
            new StreakStep(Day.AddDays(4), [2])
        ]);

        result.LengthPerStep.Should().Equal(1, 2, 1, 0, 1);
        result.Runs.OrderBy(x => x.From).ThenBy(x => x.Key).Should().Equal(
            new StreakRun(1, 2, Day, Day.AddDays(1), false),
            new StreakRun(2, 1, Day, Day, false),
            new StreakRun(2, 1, Day.AddDays(2), Day.AddDays(2), false),
            new StreakRun(2, 1, Day.AddDays(4), Day.AddDays(4), true));
    }
}
