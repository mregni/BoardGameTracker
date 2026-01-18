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
            .ReturnsAsync(new List<Badge>());

        // Act
        var result = await _controller.GetBadges();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().BeEmpty();

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBadges_ShouldReturnBadgesWithNullLevel_WhenBadgeLevelIsNull()
    {
        // Arrange
        var badges = new List<Badge>
        {
            Badge.CreateWithId(1, "badge_firsttry_title", "badge_firsttry_desc", BadgeType.FirstTry, "firsttry.png", null)
        };

        _badgeServiceMock
            .Setup(x => x.GetAllBadgesAsync())
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetBadges();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().HaveCount(1);
        returnedBadges[0].Level.Should().BeNull();

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetBadges_ShouldReturnAllBadgeLevels()
    {
        // Arrange
        var badges = new List<Badge>
        {
            Badge.CreateWithId(1, "title1", "desc1", BadgeType.Sessions, "img1.png", BadgeLevel.Green),
            Badge.CreateWithId(2, "title2", "desc2", BadgeType.Sessions, "img2.png", BadgeLevel.Blue),
            Badge.CreateWithId(3, "title3", "desc3", BadgeType.Sessions, "img3.png", BadgeLevel.Red),
            Badge.CreateWithId(4, "title4", "desc4", BadgeType.Sessions, "img4.png", BadgeLevel.Gold)
        };

        _badgeServiceMock
            .Setup(x => x.GetAllBadgesAsync())
            .ReturnsAsync(badges);

        // Act
        var result = await _controller.GetBadges();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedBadges = okResult.Value.Should().BeAssignableTo<List<BadgeDto>>().Subject;

        returnedBadges.Should().HaveCount(4);
        returnedBadges[0].Level.Should().Be(BadgeLevel.Green);
        returnedBadges[1].Level.Should().Be(BadgeLevel.Blue);
        returnedBadges[2].Level.Should().Be(BadgeLevel.Red);
        returnedBadges[3].Level.Should().Be(BadgeLevel.Gold);

        _badgeServiceMock.Verify(x => x.GetAllBadgesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
