using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Games;

public class CreateGameCommandValidationTests
{
    [Fact]
    public void Validate_ShouldPass_WhenBothRangesAreComplete()
    {
        var command = new CreateGameCommand { Title = "Game", MinPlayers = 2, MaxPlayers = 4, MinPlayTime = 30, MaxPlayTime = 60 };

        Validate(command).Should().BeEmpty();
    }

    [Fact]
    public void Validate_ShouldPass_WhenNoRangeIsGiven()
    {
        var command = new CreateGameCommand { Title = "Game" };

        Validate(command).Should().BeEmpty();
    }

    [Theory]
    [InlineData(2, null)]
    [InlineData(null, 4)]
    public void Validate_ShouldFail_WhenOnlyOnePlayerBoundIsGiven(int? min, int? max)
    {
        var command = new CreateGameCommand { Title = "Game", MinPlayers = min, MaxPlayers = max };

        var results = Validate(command);

        results.Should().ContainSingle().Which.ErrorMessage.Should().Be(Constants.Errors.PlayerRangeIncomplete);
        results.Single().MemberNames.Should().BeEquivalentTo(nameof(CreateGameCommand.MinPlayers), nameof(CreateGameCommand.MaxPlayers));
    }

    [Theory]
    [InlineData(30, null)]
    [InlineData(null, 60)]
    public void Validate_ShouldFail_WhenOnlyOnePlayTimeBoundIsGiven(int? min, int? max)
    {
        var command = new CreateGameCommand { Title = "Game", MinPlayTime = min, MaxPlayTime = max };

        var results = Validate(command);

        results.Should().ContainSingle().Which.ErrorMessage.Should().Be(Constants.Errors.PlayTimeRangeIncomplete);
    }

    [Fact]
    public void Validate_ShouldFail_WhenMinimumIsAboveMaximum()
    {
        var command = new CreateGameCommand { Title = "Game", MinPlayers = 5, MaxPlayers = 4, MinPlayTime = 90, MaxPlayTime = 60 };

        var results = Validate(command);

        results.Should().HaveCount(2);
        results.Select(x => x.ErrorMessage).Should().AllBe(Constants.Errors.RangeMinAboveMax);
    }

    private static List<ValidationResult> Validate(CreateGameCommand command)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(command, new ValidationContext(command), results, validateAllProperties: true);
        return results;
    }
}
