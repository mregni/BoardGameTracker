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

    [Fact]
    public void MaxSuggestions_ShouldKeepItsValue()
    {
        PlayerLinkMatcher.MaxSuggestions.Should().Be(3);
    }

    [Theory]
    [InlineData("mikhael@example.com", true, null, true)]
    [InlineData("mikhael@example.com", false, "  ", true)]
    [InlineData(null, false, null, true)]
    [InlineData("mikhael@example.com", true, " MIKHAEL@example.com ", true)]
    [InlineData("mikhael@example.com", false, "mikhael@example.com", false)]
    [InlineData("mikhael@example.com", true, "someone@example.com", false)]
    [InlineData(null, true, "mikhael@example.com", false)]
    public void EmailAllowsLink_ShouldOnlyAcceptAnEmptyPlayerEmailOrTheConfirmedAccountEmail(string? account, bool confirmed, string? player, bool expected)
    {
        PlayerLinkMatcher.EmailAllowsLink(account, confirmed, player).Should().Be(expected);
    }

    [Theory]
    [InlineData("Mikhael_Regni")]
    [InlineData("mikhael-regni")]
    [InlineData("Mikhael.Regni")]
    [InlineData("Mikhael,Regni")]
    [InlineData("Mikhael\tRegni")]
    public void Suggest_ShouldSplitNamesOnEverySeparator(string playerName)
    {
        var result = PlayerLinkMatcher.Suggest([Player(1, playerName)], null, "Mikhaël Regni", null);

        result.Should().ContainSingle().Which.Reason.Should().Be(PlayerLinkMatchReason.Name);
    }

    [Theory]
    [InlineData("Mi", "Mi Xiao", PlayerLinkMatchReason.Partial)]
    [InlineData("Xiao Mi", "Mi", PlayerLinkMatchReason.Partial)]
    [InlineData("M Xiao", "M Chen", null)]
    [InlineData("M", "Xiao M", null)]
    public void Suggest_ShouldNeedTwoLettersForAPartialMatch(string playerName, string displayName, PlayerLinkMatchReason? expected)
    {
        var result = PlayerLinkMatcher.Suggest([Player(1, playerName)], null, displayName, null);

        result.Select(r => (PlayerLinkMatchReason?)r.Reason).Should().Equal(expected == null ? [] : [expected]);
    }

    [Theory]
    [InlineData("Regni Junior", PlayerLinkMatchReason.Partial)]
    [InlineData("Junior Regni", null)]
    [InlineData("Regni Mikhael Junior", null)]
    public void Suggest_ShouldOnlyMatchWordsNextToEachOtherInTheSameOrder(string playerName, PlayerLinkMatchReason? expected)
    {
        var result = PlayerLinkMatcher.Suggest([Player(1, playerName)], null, "Mikhael Regni Junior", null);

        result.Select(r => (PlayerLinkMatchReason?)r.Reason).Should().Equal(expected == null ? [] : [expected]);
    }

    [Fact]
    public void Suggest_ShouldRankAPartialDisplayNameBeforeAPartialUsername()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Anna"), Player(2, "Mikhael") };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhael Regni", "anna.smith");

        result.Select(r => r.Id).Should().Equal(2, 1);
    }

    [Fact]
    public void Suggest_ShouldRankAUsernameMatchBeforeAPartialDisplayName()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "Mikhael"), Player(2, "Anna Smith") };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhael Regni", "anna.smith");

        result.Select(r => r.Id).Should().Equal(2, 1);
        result.Select(r => r.Reason).Should().Equal(PlayerLinkMatchReason.Name, PlayerLinkMatchReason.Partial);
    }

    [Fact]
    public void Suggest_ShouldOrderEqualNamesById()
    {
        var players = new List<PlayerLinkCandidate> { Player(2, "Mikhael"), Player(1, "MIKHAEL") };

        var result = PlayerLinkMatcher.Suggest(players, null, "Mikhael", null);

        result.Select(r => r.Id).Should().Equal(1, 2);
    }

    [Fact]
    public void Suggest_ShouldKeepAUsernameThatStartsWithAnAtSign()
    {
        var result = PlayerLinkMatcher.Suggest([Player(1, "mregni")], null, null, "@mregni");

        result.Should().BeEmpty();
    }

    [Fact]
    public void Suggest_ShouldSkipAPlayerWhoseNameHasNoWords_UnlessTheEmailMatches()
    {
        var players = new List<PlayerLinkCandidate> { Player(1, "--"), Player(2, "..", "mikhael@example.com") };

        var result = PlayerLinkMatcher.Suggest(players, "mikhael@example.com", "Mikhael", null);

        result.Select(r => r.Id).Should().Equal(2);
    }
}
