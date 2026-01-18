using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.BadgeEvaluators;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Evaluators;

public class SoloSpecialistBadgeEvaluatorTests
{
    private readonly SoloSpecialistBadgeEvaluator _evaluator;
    private const int PlayerId = 1;

    public SoloSpecialistBadgeEvaluatorTests()
    {
        _evaluator = new SoloSpecialistBadgeEvaluator();
    }

    [Fact]
    public void BadgeType_ShouldBeSoloSpecialist()
    {
        _evaluator.BadgeType.Should().Be(BadgeType.SoloSpecialist);
    }

    #region Green Level Tests (5 solo sessions)

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnFalse_WhenSoloSessionCountIsLessThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSoloSessions(4);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenSoloSessionCountIsExactly5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSoloSessions(5);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GreenLevel_ShouldReturnTrue_WhenSoloSessionCountIsMoreThan5()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = CreateSoloSessions(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Blue Level Tests (10 solo sessions)

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnFalse_WhenSoloSessionCountIsLessThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSoloSessions(9);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenSoloSessionCountIsExactly10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSoloSessions(10);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_BlueLevel_ShouldReturnTrue_WhenSoloSessionCountIsMoreThan10()
    {
        var badge = CreateBadge(BadgeLevel.Blue);
        var sessions = CreateSoloSessions(15);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Red Level Tests (25 solo sessions)

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnFalse_WhenSoloSessionCountIsLessThan25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSoloSessions(24);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenSoloSessionCountIsExactly25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSoloSessions(25);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_RedLevel_ShouldReturnTrue_WhenSoloSessionCountIsMoreThan25()
    {
        var badge = CreateBadge(BadgeLevel.Red);
        var sessions = CreateSoloSessions(35);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Gold Level Tests (50 solo sessions)

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnFalse_WhenSoloSessionCountIsLessThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSoloSessions(49);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenSoloSessionCountIsExactly50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSoloSessions(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Fact]
    public async Task CanAwardBadge_GoldLevel_ShouldReturnTrue_WhenSoloSessionCountIsMoreThan50()
    {
        var badge = CreateBadge(BadgeLevel.Gold);
        var sessions = CreateSoloSessions(60);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    #endregion

    #region Edge Cases

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenBadgeLevelIsNull()
    {
        var badge = Badge.CreateWithId(1, "title", "desc", BadgeType.SoloSpecialist, "image", null);
        var sessions = CreateSoloSessions(50);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Fact]
    public async Task CanAwardBadge_ShouldOnlyCountSoloSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 3 solo sessions
        for (var i = 0; i < 3; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }

        // Create 5 multi-player sessions
        for (var i = 3; i < 8; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            session.AddPlayerSession(2, null, false, false); // Second player
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse(); // Only 3 solo sessions, needs 5
    }

    [Fact]
    public async Task CanAwardBadge_ShouldReturnFalse_WhenNoSoloSessions()
    {
        var badge = CreateBadge(BadgeLevel.Green);
        var sessions = new List<Session>();

        // Create 10 multi-player sessions
        for (var i = 0; i < 10; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            session.AddPlayerSession(2, null, false, false);
            sessions.Add(session);
        }

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 5)]
    [InlineData(BadgeLevel.Blue, 10)]
    [InlineData(BadgeLevel.Red, 25)]
    [InlineData(BadgeLevel.Gold, 50)]
    public async Task CanAwardBadge_ShouldReturnTrue_AtExactThreshold(BadgeLevel level, int soloCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSoloSessions(soloCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeTrue();
    }

    [Theory]
    [InlineData(BadgeLevel.Green, 4)]
    [InlineData(BadgeLevel.Blue, 9)]
    [InlineData(BadgeLevel.Red, 24)]
    [InlineData(BadgeLevel.Gold, 49)]
    public async Task CanAwardBadge_ShouldReturnFalse_JustBelowThreshold(BadgeLevel level, int soloCount)
    {
        var badge = CreateBadge(level);
        var sessions = CreateSoloSessions(soloCount);

        var result = await _evaluator.CanAwardBadge(PlayerId, badge, sessions[0], sessions);

        result.Should().BeFalse();
    }

    #endregion

    #region Helper Methods

    private static Badge CreateBadge(BadgeLevel? level)
    {
        return Badge.CreateWithId(1, "solo_specialist_title", "solo_specialist_desc", BadgeType.SoloSpecialist, "badge.png", level);
    }

    private static Session CreateSession(int gameId, int dayOffset)
    {
        var start = DateTime.UtcNow.AddDays(-dayOffset).AddHours(-2);
        var end = DateTime.UtcNow.AddDays(-dayOffset);
        return new Session(gameId, start, end, $"Session {dayOffset}");
    }

    private static List<Session> CreateSoloSessions(int count)
    {
        var sessions = new List<Session>();
        for (var i = 0; i < count; i++)
        {
            var session = CreateSession(1, i);
            session.AddPlayerSession(PlayerId, null, false, true);
            sessions.Add(session);
        }
        return sessions;
    }

    #endregion
}
