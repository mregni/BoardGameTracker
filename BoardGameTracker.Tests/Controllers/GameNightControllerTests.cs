using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.GameNights.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class GameNightControllerTests
{
    private readonly Mock<IGameNightService> _gameNightServiceMock;
    private readonly GameNightController _controller;

    public GameNightControllerTests()
    {
        _gameNightServiceMock = new Mock<IGameNightService>();
        _controller = new GameNightController(_gameNightServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _gameNightServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnOkWithGameNights_WhenGameNightsExist()
    {
        var startDate = new DateTime(2026, 4, 1, 18, 0, 0, DateTimeKind.Utc);
        var gameNights = new List<GameNight>
        {
            GameNight.Create("Game Night Alpha", "Bring snacks", startDate, 1, 1),
            GameNight.Create("Game Night Beta", "", startDate.AddDays(7), 2, 2)
        };

        _gameNightServiceMock
            .Setup(x => x.GetGameNights())
            .ReturnsAsync(gameNights);

        var result = await _controller.GetGameNights();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGameNights = okResult.Value.Should().BeAssignableTo<List<GameNightDto>>().Subject;

        returnedGameNights.Should().HaveCount(2);
        returnedGameNights[0].Title.Should().Be("Game Night Alpha");
        returnedGameNights[1].Title.Should().Be("Game Night Beta");

        _gameNightServiceMock.Verify(x => x.GetGameNights(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameNights_ShouldReturnOkWithEmptyList_WhenNoGameNightsExist()
    {
        _gameNightServiceMock
            .Setup(x => x.GetGameNights())
            .ReturnsAsync([]);

        var result = await _controller.GetGameNights();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var returnedGameNights = okResult.Value.Should().BeAssignableTo<List<GameNightDto>>().Subject;

        returnedGameNights.Should().BeEmpty();

        _gameNightServiceMock.Verify(x => x.GetGameNights(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldReturnOkWithCreatedGameNight_WhenCommandIsValid()
    {
        var command = new CreateGameNightCommand
        {
            Title = "New Game Night",
            Notes = "First edition",
            StartDate = new DateTime(2026, 5, 10, 19, 0, 0, DateTimeKind.Utc),
            HostId = 1,
            LocationId = 2
        };

        var createdGameNight = GameNight.Create(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);

        _gameNightServiceMock
            .Setup(x => x.Create(command))
            .ReturnsAsync(createdGameNight);

        var result = await _controller.Create(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameNightDto = okResult.Value.Should().BeAssignableTo<GameNightDto>().Subject;

        gameNightDto.Title.Should().Be("New Game Night");
        gameNightDto.Notes.Should().Be("First edition");
        gameNightDto.HostId.Should().Be(1);
        gameNightDto.LocationId.Should().Be(2);

        _gameNightServiceMock.Verify(x => x.Create(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldReturnOkWithUpdatedGameNight_WhenCommandIsValid()
    {
        var command = new UpdateGameNightCommand
        {
            Id = 1,
            Title = "Updated Game Night",
            Notes = "Revised notes",
            StartDate = new DateTime(2026, 6, 15, 20, 0, 0, DateTimeKind.Utc),
            HostId = 3,
            LocationId = 4
        };

        var updatedGameNight = GameNight.Create(command.Title, command.Notes, command.StartDate, command.HostId, command.LocationId);

        _gameNightServiceMock
            .Setup(x => x.Update(command))
            .ReturnsAsync(updatedGameNight);

        var result = await _controller.Update(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameNightDto = okResult.Value.Should().BeAssignableTo<GameNightDto>().Subject;

        gameNightDto.Title.Should().Be("Updated Game Night");
        gameNightDto.Notes.Should().Be("Revised notes");
        gameNightDto.HostId.Should().Be(3);
        gameNightDto.LocationId.Should().Be(4);

        _gameNightServiceMock.Verify(x => x.Update(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldReturnNoContent_WhenGameNightExists()
    {
        var gameNightId = 1;
        var gameNight = GameNight.Create("Game Night", "", DateTime.UtcNow, 1, 1);

        _gameNightServiceMock
            .Setup(x => x.GetById(gameNightId))
            .ReturnsAsync(gameNight);

        _gameNightServiceMock
            .Setup(x => x.Delete(gameNightId))
            .Returns(Task.CompletedTask);

        var result = await _controller.Delete(gameNightId);

        result.Should().BeOfType<NoContentResult>();

        _gameNightServiceMock.Verify(x => x.GetById(gameNightId), Times.Once);
        _gameNightServiceMock.Verify(x => x.Delete(gameNightId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldReturnNotFound_WhenGameNightDoesNotExist()
    {
        var gameNightId = 999;

        _gameNightServiceMock
            .Setup(x => x.GetById(gameNightId))
            .ReturnsAsync((GameNight?)null);

        var result = await _controller.Delete(gameNightId);

        result.Should().BeOfType<NotFoundResult>();

        _gameNightServiceMock.Verify(x => x.GetById(gameNightId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldReturnOkWithRsvpDto_WhenCommandHasId()
    {
        var command = new UpdateRsvpCommand
        {
            Id = 5,
            State = GameNightRsvpState.Accepted
        };

        var rsvp = GameNightRsvp.Create(1, GameNightRsvpState.Accepted);

        _gameNightServiceMock
            .Setup(x => x.UpdateRsvp(command))
            .ReturnsAsync(rsvp);

        var result = await _controller.UpdateRsvp(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<GameNightRsvpDto>();

        _gameNightServiceMock.Verify(x => x.UpdateRsvp(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateRsvp_ShouldReturnOkWithRsvpDto_WhenCommandHasGameNightIdAndPlayerId()
    {
        var command = new UpdateRsvpCommand
        {
            GameNightId = 10,
            PlayerId = 3,
            State = GameNightRsvpState.Declined
        };

        var rsvp = GameNightRsvp.Create(3, GameNightRsvpState.Declined);

        _gameNightServiceMock
            .Setup(x => x.UpdateRsvp(command))
            .ReturnsAsync(rsvp);

        var result = await _controller.UpdateRsvp(command);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        okResult.Value.Should().BeAssignableTo<GameNightRsvpDto>();

        _gameNightServiceMock.Verify(x => x.UpdateRsvp(command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLink_ShouldReturnOkWithGameNightDto_WhenLinkIdExists()
    {
        var linkId = Guid.NewGuid();
        var gameNight = GameNight.Create("Link Night", "Found via link", DateTime.UtcNow, 1, 1);

        _gameNightServiceMock
            .Setup(x => x.GetByLinkId(linkId))
            .ReturnsAsync(gameNight);

        var result = await _controller.GetByLink(linkId);

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var gameNightDto = okResult.Value.Should().BeAssignableTo<GameNightDto>().Subject;

        gameNightDto.Title.Should().Be("Link Night");

        _gameNightServiceMock.Verify(x => x.GetByLinkId(linkId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetByLink_ShouldReturnNotFound_WhenLinkIdDoesNotExist()
    {
        var linkId = Guid.NewGuid();

        _gameNightServiceMock
            .Setup(x => x.GetByLinkId(linkId))
            .ReturnsAsync((GameNight?)null);

        var result = await _controller.GetByLink(linkId);

        result.Should().BeOfType<NotFoundResult>();

        _gameNightServiceMock.Verify(x => x.GetByLinkId(linkId), Times.Once);
        VerifyNoOtherCalls();
    }
}
