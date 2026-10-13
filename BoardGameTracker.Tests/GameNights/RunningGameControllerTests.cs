using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.GameNights.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.GameNights;

public class RunningGameControllerTests
{
    private static readonly DateTime Start = new(2026, 10, 16, 22, 5, 0, DateTimeKind.Utc);

    private readonly Mock<IRunningGameService> _runningGameServiceMock;
    private readonly RunningGameController _controller;

    public RunningGameControllerTests()
    {
        _runningGameServiceMock = new Mock<IRunningGameService>();
        _controller = new RunningGameController(_runningGameServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _runningGameServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Start_ShouldReturnCreatedWithTheRunningGame()
    {
        var command = new StartRunningGameCommand { GameId = 9, PlayerIds = [1, 2] };
        var runningGame = RunningGame.Create(3, 9, Start, [1, 2]);
        runningGame.Id = 12;
        _runningGameServiceMock.Setup(x => x.Start(3, command)).ReturnsAsync(runningGame);

        var result = await _controller.Start(3, command);

        var created = result.Should().BeOfType<CreatedResult>().Subject;
        var dto = created.Value.Should().BeOfType<RunningGameDto>().Subject;
        dto.Id.Should().Be(12);
        dto.GameNightId.Should().Be(3);
        dto.GameId.Should().Be(9);
        dto.Start.Should().Be(Start);
        dto.PlayerIds.Should().Equal(1, 2);

        _runningGameServiceMock.Verify(x => x.Start(3, command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Finish_ShouldReturnTheCreatedSession()
    {
        var command = new FinishRunningGameCommand { PlayerSessions = [new CreatePlayerSessionCommand { PlayerId = 1, Won = true }] };
        var session = new Session(9, Start, Start.AddMinutes(37), string.Empty) { Id = 40 };
        session.SetGameNight(3);
        _runningGameServiceMock.Setup(x => x.Finish(12, command)).ReturnsAsync(session);

        var result = await _controller.Finish(12, command);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<SessionDto>().Subject;
        dto.Id.Should().Be(40);
        dto.GameNightId.Should().Be(3);
        dto.Minutes.Should().Be(37);

        _runningGameServiceMock.Verify(x => x.Finish(12, command), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Discard_ShouldReturnNoContent_WhenTheRunningGameExists()
    {
        _runningGameServiceMock.Setup(x => x.GetById(12)).ReturnsAsync(RunningGame.Create(3, 9, Start, [1]));

        var result = await _controller.Discard(12);

        result.Should().BeOfType<NoContentResult>();
        _runningGameServiceMock.Verify(x => x.GetById(12), Times.Once);
        _runningGameServiceMock.Verify(x => x.Delete(12), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Discard_ShouldReturnNotFound_WhenTheRunningGameDoesNotExist()
    {
        _runningGameServiceMock.Setup(x => x.GetById(12)).ReturnsAsync((RunningGame?)null);

        var result = await _controller.Discard(12);

        result.Should().BeOfType<NotFoundResult>();
        _runningGameServiceMock.Verify(x => x.GetById(12), Times.Once);
        VerifyNoOtherCalls();
    }
}
