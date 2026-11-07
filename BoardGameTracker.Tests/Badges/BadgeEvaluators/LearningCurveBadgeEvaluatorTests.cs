using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Badges.BadgeEvaluators;

public class LearningCurveBadgeEvaluatorTests
{
    private readonly LearningCurveBadgeEvaluator _evaluator;
    private const int PlayerId = 1;
    private const int GameId = 1;

    public LearningCurveBadgeEvaluatorTests()
    {
        _evaluator = new LearningCurveBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeLearningCurve()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.LearningCurve);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenLessThan3Sessions()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    public static TheoryData<double[], bool> ScorePatterns => new()
    {
        { [100.0, 90.0, 80.0], true },
        { [80.0, 90.0, 100.0], false },
        { [100.0, 100.0, 100.0], false },
        { [95.0, 100.0, 90.0], false },
        { [100.0, 80.0, 90.0], false },
        { [100.0, 90.0, 90.0], false },
        { [1000.0, 500.0, 100.0], true },
        { [100.1, 100.05, 100.0], true }
    };

    [Theory]
    [MemberData(nameof(ScorePatterns))]
    public async Task CanAwardBadge_ShouldEvaluateScoreProgression(double[] scoresMostRecentFirst, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores(scoresMostRecentFirst);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAnyScoreIsNull()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            double? score = i == 1 ? null : 100 - i * 10;
            sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(i).WithPlayer(PlayerId, score).Build());
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenAllScoresAreNull()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 3; i++)
        {
            var session = new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithPlayer(PlayerId)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyConsiderSessionsOfCurrentGame()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();

        for (var i = 0; i < 2; i++)
        {
            var session = new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithPlayer(PlayerId, 100 - i * 10)
                .Build();
            sessions.Add(session);
        }

        for (var i = 2; i < 5; i++)
        {
            var session = new SessionBuilder().ForGame(GameId + i).DaysAgo(i)
                .WithPlayer(PlayerId, 100 - i * 10)
                .Build();
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyUseThreeMostRecentSessions()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0, 80.0, 95.0, 85.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOrderSessionsByStartDate_NotListOrder()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var newest = new SessionBuilder().ForGame(GameId)
            .WithPlayer(PlayerId, 100)
            .Build();
        var middle = new SessionBuilder().ForGame(GameId).DaysAgo(1)
            .WithPlayer(PlayerId, 90)
            .Build();
        var oldest = new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithPlayer(PlayerId, 80)
            .Build();
        var sessions = new List<Session> { oldest, newest, middle };

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, newest, sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task CanAwardBadge_ShouldIgnoreBadgeLevel(BadgeLevel? level)
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, level);
        var sessions = CreateSessionsWithScores([100.0, 90.0, 80.0]);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(new[] { 30.0, 40.0, 50.0 }, true)]
    [InlineData(new[] { 50.0, 40.0, 30.0 }, false)]
    [InlineData(new[] { 40.0, 40.0, 50.0 }, false)]
    [InlineData(new[] { 30.0, 40.0, 40.0 }, false)]
    [InlineData(new[] { 30.0, 40.0, 35.0 }, false)]
    public async Task CanAwardBadge_ShouldTreatLowerScoresAsBetter_WhenTheLowestScoreWins(double[] scoresMostRecentFirst, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();
        for (var i = 0; i < scoresMostRecentFirst.Length; i++)
        {
            sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithWinner(PlayerId, scoresMostRecentFirst[i])
                .WithPlayer(PlayerId + 1, 100)
                .Build());
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Theory]
    [InlineData(new[] { 50.0, 40.0, 30.0 }, true)]
    [InlineData(new[] { 30.0, 40.0, 50.0 }, false)]
    public async Task CanAwardBadge_ShouldTreatHigherScoresAsBetter_WhenTheHighestScoreWins(double[] scoresMostRecentFirst, bool expected)
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();
        for (var i = 0; i < scoresMostRecentFirst.Length; i++)
        {
            sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithWinner(PlayerId, scoresMostRecentFirst[i])
                .WithPlayer(PlayerId + 1, 10)
                .Build());
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().Be(expected);
    }

    [Fact]
    public async Task CanAwardBadge_ShouldKeepHigherIsBetter_WhenAWinnerTiesWithALoser()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithWinner(PlayerId, 80)
            .WithPlayer(PlayerId + 1, 80)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldKeepHigherIsBetter_WhenAnyWinnerScoresAboveALoser()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithWinner(PlayerId, 80)
            .WithWinner(PlayerId + 1, 200)
            .WithPlayer(PlayerId + 2, 150)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldKeepHigherIsBetter_WhenALoserScoresBelowTheWinner()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithWinner(PlayerId, 80)
            .WithPlayer(PlayerId + 1, 200)
            .WithPlayer(PlayerId + 2, 70)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldKeepHigherIsBetter_WhenOnlyLosersHaveAScore()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithPlayer(PlayerId, 80)
            .WithWinner(PlayerId + 1)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldKeepHigherIsBetter_WhenOnlyWinnersHaveAScore()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(2)
            .WithWinner(PlayerId, 80)
            .WithPlayer(PlayerId + 1)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldTreatLowerScoresAsBetter_WhenOnlyAnOlderSessionShowsTheLowestWins()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([30.0, 40.0, 50.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(3)
            .WithWinner(PlayerId, 60)
            .WithPlayer(PlayerId + 1, 100)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldIgnoreTheScoringDirectionOfOtherGames()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = CreateSessionsWithScores([100.0, 90.0, 80.0]);
        sessions.Add(new SessionBuilder().ForGame(GameId + 1).DaysAgo(3)
            .WithWinner(PlayerId, 60)
            .WithPlayer(PlayerId + 1, 100)
            .Build());

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReadTheScoreOfTheEvaluatedPlayer()
    {
        var badge = TestBadges.Create(BadgeType.LearningCurve, BadgeLevel.Green);
        var sessions = new List<Session>();
        for (var i = 0; i < 3; i++)
        {
            sessions.Add(new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithPlayer(PlayerId + 1, 100 - i * 10)
                .WithPlayer(PlayerId, 50 + i * 10)
                .Build());
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    private static List<Session> CreateSessionsWithScores(double[] scoresMostRecentFirst)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < scoresMostRecentFirst.Length; i++)
        {
            var session = new SessionBuilder().ForGame(GameId).DaysAgo(i)
                .WithPlayer(PlayerId, scoresMostRecentFirst[i])
                .Build();
            sessions.Add(session);
        }
        return sessions;
    }
}
