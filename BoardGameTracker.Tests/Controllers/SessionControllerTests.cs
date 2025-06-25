using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AutoMapper;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.ViewModels;
using BoardGameTracker.Common.ViewModels.Results;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class SessionControllerTests
    {
        private readonly Mock<ISessionService> _sessionServiceMock;
        private readonly Mock<IMapper> _mapperMock;
        private readonly Mock<ILogger<SessionController>> _loggerMock;
        private readonly SessionController _controller;
        private readonly Mock<IGameService> _gameServiceMock;

        public SessionControllerTests()
        {
            _sessionServiceMock = new Mock<ISessionService>();
            _mapperMock = new Mock<IMapper>();
            _loggerMock = new Mock<ILogger<SessionController>>();
            _gameServiceMock = new Mock<IGameService>();
            
            _controller = new SessionController(
                _sessionServiceMock.Object, 
                _mapperMock.Object, 
                _loggerMock.Object, 
                _gameServiceMock.Object);
        }

        [Fact]
        public async Task GetSession_ShouldReturnOkResultWithMappedSession_WhenSessionExists()
        {
            const int sessionId = 1;
            var session = new Session 
            { 
                Id = sessionId, 
                Comment = "Great game", 
                GameId = 1, 
                Start = DateTime.Now,
                End = DateTime.Now.AddHours(2),
                LocationId = 1
            };
            var sessionViewModel = new SessionViewModel 
            { 
                Id = "1", 
                Comment = "Great game", 
                GameId = "1",
                Start = session.Start,
                Minutes = 120,
                LocationId = "1",
                Flags = [SessionFlag.LongestGame]
            };

            _sessionServiceMock.Setup(x => x.Get(sessionId)).ReturnsAsync(session);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(session)).Returns(sessionViewModel);

            var result = await _controller.GetSession(sessionId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(sessionViewModel);

            _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(session), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetSession_ShouldReturnNotFound_WhenSessionDoesNotExist()
        {
            const int sessionId = 1;

            _sessionServiceMock.Setup(x => x.Get(sessionId)).ReturnsAsync((Session?)null);

            var result = await _controller.GetSession(sessionId);

            result.Should().BeOfType<NotFoundResult>();

            _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task GetSession_ShouldHandleDifferentIds_WhenValidIdsProvided(int sessionId)
        {
            var session = new Session { Id = sessionId, Comment = "Test", GameId = 1 };
            var sessionViewModel = new SessionViewModel { Id = sessionId.ToString(), Comment = "Test", GameId = "1" };

            _sessionServiceMock.Setup(x => x.Get(sessionId)).ReturnsAsync(session);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(session)).Returns(sessionViewModel);

            var result = await _controller.GetSession(sessionId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(sessionViewModel);

            _sessionServiceMock.Verify(x => x.Get(sessionId), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(session), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateSession_ShouldReturnOkResultWithMappedSession_WhenValidViewModelProvided()
        {
            var viewModel = new SessionViewModel 
            { 
                Comment = "New session", 
                GameId = "1",
                Start = DateTime.Now,
                Minutes = 90,
                LocationId = "1",
                Flags = [SessionFlag.HighestScore],
                ExpansionIds = []
            };
            var session = new Session 
            { 
                Comment = "New session", 
                GameId = 1,
                Start = viewModel.Start,
                End = viewModel.Start.AddMinutes(90),
                LocationId = 1
            };
            var createdSession = new Session 
            { 
                Id = 1,
                Comment = "New session", 
                GameId = 1,
                Start = viewModel.Start,
                End = viewModel.Start.AddMinutes(90),
                LocationId = 1
            };
            var resultViewModel = new SessionViewModel 
            { 
                Id = "1",
                Comment = "New session", 
                GameId = "1",
                Start = viewModel.Start,
                Minutes = 90,
                LocationId = "1",
                Flags = [SessionFlag.HighestScore]
            };

            _mapperMock.Setup(x => x.Map<Session>(viewModel)).Returns(session);
            _sessionServiceMock.Setup(x => x.Create(session)).ReturnsAsync(createdSession);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(createdSession)).Returns(resultViewModel);

            var result = await _controller.CreateSession(viewModel);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resultViewModel);

            _mapperMock.Verify(x => x.Map<Session>(viewModel), Times.Once);
            _sessionServiceMock.Verify(x => x.Create(session), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(createdSession), Times.Once);
            _gameServiceMock.Verify(x => x.GetGameExpansions(new List<int>()), Times.Once);

            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateSession_ShouldReturnBadRequest_WhenViewModelIsNull()
        {
            var result = await _controller.CreateSession(null);

            result.Should().BeOfType<BadRequestResult>();

            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateSession_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            var viewModel = new SessionViewModel { Comment = "Test", GameId = "1", ExpansionIds = []};
            var session = new Session { Comment = "Test", GameId = 1 };
            var expectedException = new InvalidOperationException("Service error");

            _mapperMock.Setup(x => x.Map<Session>(viewModel)).Returns(session);
            _sessionServiceMock.Setup(x => x.Create(session)).ThrowsAsync(expectedException);

            var result = await _controller.CreateSession(viewModel);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(500);

            _mapperMock.Verify(x => x.Map<Session>(viewModel), Times.Once);
            _sessionServiceMock.Verify(x => x.Create(session), Times.Once);
            _gameServiceMock.Verify(x => x.GetGameExpansions(new List<int>()), Times.Once);
            
            _mapperMock.VerifyNoOtherCalls();
            _sessionServiceMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
            VerifyLoggerErrorCalled(expectedException.Message);
        }
        
        [Fact]
        public async Task CreateSession_ShouldSetExpansionListCorrectly()
        {
            var expansionIds = new List<int> { 1, 2 };
            var viewModel = new SessionViewModel { Comment = "Test", GameId = "1", ExpansionIds = expansionIds};
            var resultViewModel = new SessionViewModel { Id = "1", Comment = "New session" };
            var expansionList = new List<Expansion>()
            {
                new Expansion(), new Expansion()
            };
            var session = new Session { Comment = "Test", GameId = 1, Expansions = expansionList};

            _mapperMock.Setup(x => x.Map<Session>(viewModel)).Returns(session);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(session)).Returns(resultViewModel);
            _sessionServiceMock.Setup(x => x.Create(session)).ReturnsAsync(session);
            _gameServiceMock.Setup(x => x.GetGameExpansions(expansionIds)).ReturnsAsync(expansionList);
            
            var result = await _controller.CreateSession(viewModel);

            result.Should().BeOfType<OkObjectResult>();

            _mapperMock.Verify(x => x.Map<Session>(viewModel), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(session), Times.Once);
            _sessionServiceMock.Verify(x => x.Create(session), Times.Once);
            _gameServiceMock.Verify(x => x.GetGameExpansions(expansionIds), Times.Once);
            
            _mapperMock.VerifyNoOtherCalls();
            _sessionServiceMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateSession_ShouldReturnOkResultWithMappedSession_WhenValidViewModelProvided()
        {
            var updateViewModel = new SessionViewModel 
            { 
                Id = "1",
                Comment = "Updated session", 
                GameId = "1",
                Start = DateTime.Now,
                Minutes = 120,
                LocationId = "2",
                Flags = [SessionFlag.LongestGame]
            };
            var session = new Session 
            { 
                Id = 1,
                Comment = "Updated session", 
                GameId = 1,
                Start = updateViewModel.Start,
                End = updateViewModel.Start.AddMinutes(120),
                LocationId = 2
            };
            var updatedSession = new Session 
            { 
                Id = 1,
                Comment = "Updated session", 
                GameId = 1,
                Start = updateViewModel.Start,
                End = updateViewModel.Start.AddMinutes(120),
                LocationId = 2
            };
            var resultViewModel = new SessionViewModel 
            { 
                Id = "1",
                Comment = "Updated session", 
                GameId = "1",
                Start = updateViewModel.Start,
                Minutes = 120,
                LocationId = "2",
                Flags = [SessionFlag.LongestGame]
            };

            _mapperMock.Setup(x => x.Map<Session>(updateViewModel)).Returns(session);
            _sessionServiceMock.Setup(x => x.Update(session)).ReturnsAsync(updatedSession);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(updatedSession)).Returns(resultViewModel);

            var result = await _controller.UpdateSession(updateViewModel);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resultViewModel);

            _mapperMock.Verify(x => x.Map<Session>(updateViewModel), Times.Once);
            _sessionServiceMock.Verify(x => x.Update(session), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(updatedSession), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateSession_ShouldReturnBadRequest_WhenViewModelIsNull()
        {
            var result = await _controller.UpdateSession(null);

            result.Should().BeOfType<BadRequestResult>();

            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateSession_ShouldReturnBadRequest_WhenViewModelIdIsNull()
        {
            var updateViewModel = new SessionViewModel { Id = null, Comment = "Test", GameId = "1" };

            var result = await _controller.UpdateSession(updateViewModel);

            result.Should().BeOfType<BadRequestResult>();

            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task UpdateSession_ShouldReturnInternalServerError_WhenServiceThrowsException()
        {
            var updateViewModel = new SessionViewModel { Id = "1", Comment = "Test", GameId = "1" };
            var session = new Session { Id = 1, Comment = "Test", GameId = 1 };
            var expectedException = new InvalidOperationException("Update failed");

            _mapperMock.Setup(x => x.Map<Session>(updateViewModel)).Returns(session);
            _sessionServiceMock.Setup(x => x.Update(session)).ThrowsAsync(expectedException);

            var result = await _controller.UpdateSession(updateViewModel);

            result.Should().BeOfType<StatusCodeResult>();
            var statusResult = result as StatusCodeResult;
            statusResult!.StatusCode.Should().Be(500);

            _mapperMock.Verify(x => x.Map<Session>(updateViewModel), Times.Once);
            _sessionServiceMock.Verify(x => x.Update(session), Times.Once);
            _mapperMock.VerifyNoOtherCalls();
            _sessionServiceMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
            VerifyLoggerErrorCalled(expectedException.Message);
        }

        [Fact]
        public async Task DeleteSession_ShouldReturnOkResultWithSuccessResult_WhenSessionDeleted()
        {
            const int sessionId = 1;

            _sessionServiceMock.Setup(x => x.Delete(sessionId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteSession(sessionId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().BeOfType<DeletionResultViewModel>();
            var deletionResult = okResult.Value as DeletionResultViewModel;
            deletionResult!.Type.Should().Be((int)ResultState.Success);

            _sessionServiceMock.Verify(x => x.Delete(sessionId), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(1)]
        [InlineData(999)]
        [InlineData(int.MaxValue)]
        public async Task DeleteSession_ShouldHandleDifferentIds_WhenValidIdsProvided(int sessionId)
        {
            _sessionServiceMock.Setup(x => x.Delete(sessionId)).Returns(Task.CompletedTask);

            var result = await _controller.DeleteSession(sessionId);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            var deletionResult = okResult!.Value as DeletionResultViewModel;
            deletionResult!.Type.Should().Be((int)ResultState.Success);

            _sessionServiceMock.Verify(x => x.Delete(sessionId), Times.Once);
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CreateSession_ShouldHandleNullFlags_WhenFlagsAreNull()
        {
            var viewModel = new SessionViewModel 
            { 
                Comment = "Test session", 
                GameId = "1",
                Start = DateTime.Now,
                Minutes = 60,
                Flags = null,
                ExpansionIds = []
            };
            var session = new Session { Comment = "Test session", GameId = 1 };
            var createdSession = new Session { Id = 1, Comment = "Test session", GameId = 1 };
            var resultViewModel = new SessionViewModel 
            { 
                Id = "1",
                Comment = "Test session", 
                GameId = "1",
                Flags = null
            };

            _mapperMock.Setup(x => x.Map<Session>(viewModel)).Returns(session);
            _sessionServiceMock.Setup(x => x.Create(session)).ReturnsAsync(createdSession);
            _mapperMock.Setup(x => x.Map<SessionViewModel>(createdSession)).Returns(resultViewModel);

            var result = await _controller.CreateSession(viewModel);

            result.Should().BeOfType<OkObjectResult>();
            var okResult = result as OkObjectResult;
            okResult!.Value.Should().Be(resultViewModel);

            _mapperMock.Verify(x => x.Map<Session>(viewModel), Times.Once);
            _sessionServiceMock.Verify(x => x.Create(session), Times.Once);
            _mapperMock.Verify(x => x.Map<SessionViewModel>(createdSession), Times.Once);
            
            _gameServiceMock.Verify(x => x.GetGameExpansions(new List<int>()), Times.Once);
            
            _sessionServiceMock.VerifyNoOtherCalls();
            _mapperMock.VerifyNoOtherCalls();
            _loggerMock.VerifyNoOtherCalls();
            _gameServiceMock.VerifyNoOtherCalls();
        }

        private void VerifyLoggerErrorCalled(string expectedMessage)
        {
            _loggerMock.Verify(
                x => x.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains(expectedMessage)),
                    It.IsAny<Exception>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
            _loggerMock.VerifyNoOtherCalls();
        }
    }