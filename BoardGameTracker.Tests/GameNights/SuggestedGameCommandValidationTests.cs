using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.DTOs.Commands;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class SuggestedGameCommandValidationTests
{
    [Fact]
    public void AddSuggestedGameCommand_ShouldPass_WhenBothIdsArePositive()
    {
        Validate(new AddSuggestedGameCommand { GameNightId = 1, GameId = 2 }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 2, nameof(AddSuggestedGameCommand.GameNightId))]
    [InlineData(1, 0, nameof(AddSuggestedGameCommand.GameId))]
    [InlineData(-1, 2, nameof(AddSuggestedGameCommand.GameNightId))]
    public void AddSuggestedGameCommand_ShouldReject_ANonPositiveId(int gameNightId, int gameId, string member)
    {
        Validate(new AddSuggestedGameCommand { GameNightId = gameNightId, GameId = gameId })
            .Should().ContainSingle().Which.MemberNames.Should().Contain(member);
    }

    [Fact]
    public void RemoveSuggestedGameCommand_ShouldPass_WhenBothIdsArePositive()
    {
        Validate(new RemoveSuggestedGameCommand { GameNightId = 1, GameId = 2 }).Should().BeEmpty();
    }

    [Theory]
    [InlineData(0, 2, nameof(RemoveSuggestedGameCommand.GameNightId))]
    [InlineData(1, 0, nameof(RemoveSuggestedGameCommand.GameId))]
    public void RemoveSuggestedGameCommand_ShouldReject_ANonPositiveId(int gameNightId, int gameId, string member)
    {
        Validate(new RemoveSuggestedGameCommand { GameNightId = gameNightId, GameId = gameId })
            .Should().ContainSingle().Which.MemberNames.Should().Contain(member);
    }

    private static List<ValidationResult> Validate(object command)
    {
        var results = new List<ValidationResult>();
        Validator.TryValidateObject(command, new ValidationContext(command), results, validateAllProperties: true);
        return results;
    }
}
