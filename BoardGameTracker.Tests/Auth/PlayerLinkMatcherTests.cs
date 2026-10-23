using System.Collections.Generic;
using System.Linq;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class PlayerLinkMatcherTests
{
    private static PlayerLinkCandidate Player(int id, string name, string? email = null) => new(id, name, $"/images/{id}.png", email);

    [Fact]
    public void Suggest_ShouldMatchTheEmail_IgnoringCaseAndSurroundingSpaces()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Someone", "  Mikhael@Example.com ") };

        var result = PlayerLinkMatcher.Suggest(players, "mikhael@example.COM", "Mikhaël Regni", "mregni");

        result.Should().ContainSingle();
        result[0].Id.Should().Be(1);
        result[0].Name.Should().Be("Someone");
        result[0].Image.Should().Be("/images/1.png");
        result[0].Reason.Should().Be(PlayerLinkMatchReason.Email);
    }

    [Theory]
    [InlineData("mikhaël regni")]
    [InlineData("MIKHAEL REGNI")]
    [InlineData("  Mikhael   Regni ")]
    public void Suggest_ShouldMatchTheDisplayName_IgnoringCaseAccentsAndSpaces(string playerName)
    {
        var players = new List<PlayerLinkCandidate> { Player(1, playerName) };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhaël Regni", null);

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Name);
    }

    [Theory]
    [InlineData("mregni")]
    [InlineData("mregni@example.com")]
    public void Suggest_ShouldMatchTheUsername_WhenTheDisplayNameDoesNotMatch(string userName)
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "MRegni") };

        var result = PlayerLinkMatcher.Suggest(players, null, "Somebody Else", userName);

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Name);
    }

    [Fact]
    public void Suggest_ShouldMatchAUsernameWithSeparators_AsWords()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Mikhaël Regni") };

        var result = PlayerLinkMatcher.Suggest(players, null, null, "mikhael.regni");

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Name);
    }

    [Theory]
    [InlineData("Mikhaël")]
    [InlineData("Regni")]
    [InlineData("Mikhael R")]
    [InlineData("Mikhaël Regni Junior")]
    public void Suggest_ShouldMatchPartly_OnWholeWordsOrTheFirstName(string playerName)
    {
        var players = new List<PlayerLinkCandidate> { Player(1, playerName) };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhaël Regni", null);

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Partial);
    }

    [Fact]
    public void Suggest_ShouldMatchPartlyOnTheUsername_WhenTheDisplayNameIsMissing()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Mikhaël") };

        var result = PlayerLinkMatcher.Suggest(players, null, null, "mikhael.regni");

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Partial);
    }

    [Theory]
    [InlineData("Mik")]
    [InlineData("Michael")]
    [InlineData("Regnier")]
    [InlineData("M")]
    [InlineData("Anna Regni")]
    public void Suggest_ShouldNotMatchPartsOfWordsOrOtherFirstNames(string playerName)
    {
        var players = new List<PlayerLinkCandidate> { Player(1, playerName) };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhaël Regni", null);

        result.Should().BeEmpty();
    }

    [Fact]
    public void Suggest_ShouldRankEmailBeforeNameBeforeUsernameBeforePartial()
    {
        var players = new List<PlayerLinkCandidate>
        {
            Player(1, "Mikhaël"),
            Player(2, "mregni"),
            Player(3, "Mikhaël Regni"),
            Player(4, "Other", "mikhael@example.com"),
            Player(5, "Unrelated"),
        };

        var result = PlayerLinkMatcher.Suggest(players, "mikhael@example.com", "Mikhaël Regni", "mregni");

        result.Select(r => r.Id).Should().Equal(4, 3, 2);
        result.Select(r => r.Reason).Should().Equal(PlayerLinkMatchReason.Email, PlayerLinkMatchReason.Name, PlayerLinkMatchReason.Name);
    }

    [Fact]
    public void Suggest_ShouldOrderEqualMatchesByName_AndLimitTheNumberOfSuggestions()
    {
        var players = new List<PlayerLinkCandidate>
        {
            Player(1, "Mikhaël D"),
            Player(2, "Mikhaël C"),
            Player(3, "Mikhaël B"),
            Player(4, "Mikhaël A"),
        };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhaël Regni", null);

        result.Should().HaveCount(PlayerLinkMatcher.MaxSuggestions);
        result.Select(r => r.Id).Should().Equal(4, 3, 2);
    }

    [Fact]
    public void Suggest_ShouldReturnNothing_WhenTheAccountHasNothingToMatchOn()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Mikhaël", "mikhael@example.com") };

        var result = PlayerLinkMatcher.Suggest(players, "  ", null, "  ");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Suggest_ShouldIgnorePlayersWithoutAnEmail_WhenMatchingTheEmail()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Unrelated") };

        var result = PlayerLinkMatcher.Suggest(players, "mikhael@example.com", null, null);

        result.Should().BeEmpty();
    }
}
