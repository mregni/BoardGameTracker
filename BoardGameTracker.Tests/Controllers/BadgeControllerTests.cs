using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Badges.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class BadgeControllerTests
{
    private readonly Mock<IBadgeService> _badgeServiceMock;
    private readonly BadgeController _controller;

    public BadgeControllerTests()
    {
        _badgeServiceMock = new Mock<IBadgeService>();
        _controller = new BadgeController(_badgeServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _badgeServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBadges_ShouldReturnOkWithBadges_WhenBadgesExist()
    {
        // Arrange
        var badges = new List<Badge>
        {
            Badge.CreateWithId(1, "badge_sessions_title", "badge_sessions_desc", BadgeType.Sessions, "sessions.png", BadgeLevel.Green),
            Badge.CreateWithId(2, "badge_wins_title", "badge_wins_desc", BadgeType.Wins, "wins.png", BadgeLevel.Blue)
        };

        _badgeServiceMock
            .Setup(x => x.GetAllBadgesAsync())
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetBadges();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().HaveCount(2);
        returnedBadges[0].Id.Should().Be(1);
        returnedBadges[0].TitleKey.Should().Be("badge_sessions_title");
        returnedBadges[0].Type.Should().Be(BadgeType.Sessions);
        returnedBadges[0].Level.Should().Be(BadgeLevel.Green);
        returnedBadges[1].Id.Should().Be(2);
        returnedBadges[1].Type.Should().Be(BadgeType.Wins);
        returnedBadges[1].Level.Should().Be(BadgeLevel.Blue);

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBadges_ShouldReturnOkWithEmptyList_WhenNoBadgesExist()
    {
        // Arrange
        _badgeServiceMock
            .Setup(x => x.GetAllBadgesAsync())
            .ReturnsAsync([]);

        // Act
        var result = await _controller.GetBadges();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().BeEmpty();

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData(BadgeLevel.Green)]
    [InlineData(BadgeLevel.Blue)]
    [InlineData(BadgeLevel.Red)]
    [InlineData(BadgeLevel.Gold)]
    public async Task GetBadges_ShouldReturnBadgeLevel_ForEachLevel(BadgeLevel? level)
    {
        var badges = new List<Badge>
        {
            Badge.CreateWithId(1, "badge_sessions_title", "badge_sessions_desc", BadgeType.Sessions, "sessions.png", level)
        };

        _badgeServiceMock
            .Setup(x => x.GetAllBadgesAsync())
            .ReturnsAsync(badges);

        var result = await _controller.GetBadges();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().HaveCount(1);
        returnedBadges[0].Level.Should().Be(level);

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
