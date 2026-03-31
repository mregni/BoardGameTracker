using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class SessionControllerTests
{
    private readonly Mock<ISessionService> _sessionServiceMock;
    private readonly SessionController _controller;

    public SessionControllerTests()
    {
        _sessionServiceMock = new Mock<ISessionService>();
        _controller = new SessionController(_sessionServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _sessionServiceMock.VerifyNoOtherCalls();
    }

    #region GetSession Tests

    [Fact]
    public async Task GetSession_ShouldReturnOkWithSession_WhenSessionExists()
    {
        // Arrange
        var sessionId = 1;
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Fun game night") { Id = sessionId };

        _sessionServiceMock
            .Setup(x => x.Get(sessionId))
            .ReturnsAsync(session);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sessionDto = okResult.Value.Should().BeAssignableTo<SessionDto>().Subject;

        sessionDto.Id.Should().Be(sessionId);
        sessionDto.Comment.Should().Be("Fun game night");

        _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = 999;

        _sessionServiceMock
            .Setup(x => x.Get(sessionId))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _controller.GetSession(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region CreateSession Tests

    [Fact]
    public async Task CreateSession_ShouldReturnOkWithSession_WhenCommandIsValid()
    {
        // Arrange
        var command = new CreateSessionCommand
        {
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-2),
            Minutes = 120,
            Comment = "Great game",
            PlayerSessions =
            [
                new CreatePlayerSessionCommand {PlayerId = 1, Won = true, Score = 100},
                new CreatePlayerSessionCommand {PlayerId = 2, Won = false, Score = 80}
            ]
        };

        var createdSession = new Session(command.GameId, command.Start, command.Start.AddMinutes(command.Minutes), command.Comment) { Id = 1 };

        _sessionServiceMock
            .Setup(x => x.CreateFromCommand(command))
            .ReturnsAsync(createdSession);

        // Act
        var result = await _controller.CreateSession(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sessionDto = okResult.Value.Should().BeAssignableTo<SessionDto>().Subject;

        sessionDto.Id.Should().Be(1);
        sessionDto.Comment.Should().Be("Great game");

        _sessionServiceMock.Verify(x => x.CreateFromCommand(command), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region UpdateSession Tests

    [Fact]
    public async Task UpdateSession_ShouldReturnOkWithSession_WhenCommandIsValid()
    {
        // Arrange
        var command = new UpdateSessionCommand
        {
            Id = 1,
            GameId = 1,
            Start = DateTime.UtcNow.AddHours(-3),
            Minutes = 180,
            Comment = "Updated comment",
            PlayerSessions = [new CreatePlayerSessionCommand {PlayerId = 1, Won = true, Score = 150}]
        };

        var updatedSession = new Session(command.GameId, command.Start, command.Start.AddMinutes(command.Minutes), command.Comment) { Id = command.Id };

        _sessionServiceMock
            .Setup(x => x.UpdateFromCommand(command))
            .ReturnsAsync(updatedSession);

        // Act
        var result = await _controller.UpdateSession(command);

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var sessionDto = okResult.Value.Should().BeAssignableTo<SessionDto>().Subject;

        sessionDto.Id.Should().Be(1);
        sessionDto.Comment.Should().Be("Updated comment");

        _sessionServiceMock.Verify(x => x.UpdateFromCommand(command), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region DeleteSession Tests

    [Fact]
    public async Task DeleteSession_ShouldReturnNoContent_WhenSessionIsDeleted()
    {
        // Arrange
        var sessionId = 1;
        var session = new Session(1, DateTime.UtcNow.AddHours(-2), DateTime.UtcNow, "Test") { Id = sessionId };

        _sessionServiceMock
            .Setup(x => x.Get(sessionId))
            .ReturnsAsync(session);

        _sessionServiceMock
            .Setup(x => x.Delete(sessionId))
            .Returns(Task.CompletedTask);

        // Act
        var result = await _controller.DeleteSession(sessionId);

        // Assert
        result.Should().BeOfType<NoContentResult>();

        _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
        _sessionServiceMock.Verify(x => x.Delete(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
    {
        // Arrange
        var sessionId = 999;

        _sessionServiceMock
            .Setup(x => x.Get(sessionId))
            .ReturnsAsync((Session?)null);

        // Act
        var result = await _controller.DeleteSession(sessionId);

        // Assert
        result.Should().BeOfType<NotFoundResult>();

        _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
