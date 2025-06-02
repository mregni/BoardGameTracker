using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Sessions;
using BoardGameTracker.Core.Sessions.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class SessionServiceTests
    {
        private readonly Mock<ISessionRepository> _sessionRepositoryMock;
        private readonly SessionService _sessionService;

        public SessionServiceTests()
        {
            _sessionRepositoryMock = new Mock<ISessionRepository>();
            _sessionService = new SessionService(_sessionRepositoryMock.Object);
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedSession_WhenRepositorySucceeds()
        {
            var inputSession = new Session { Comment = "New Session", GameId = 1, Start = DateTime.Now };
            var expectedSession = new Session { Id = 1, Comment = "New Session", GameId = 1, Start = DateTime.Now };

            _sessionRepositoryMock.Setup(x => x.CreateAsync(inputSession)).ReturnsAsync(expectedSession);

            var result = await _sessionService.Create(inputSession);

            result.Should().NotBeNull();
            result.Should().Be(expectedSession);
            _sessionRepositoryMock.Verify(x => x.CreateAsync(inputSession), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldPassCorrectSession_WhenCalled()
        {
            var session = new Session 
            { 
                Id = 5, 
                Comment = "Test Session", 
                GameId = 2, 
                Start = new DateTime(2023, 1, 1),
                End = new DateTime(2023, 1, 1, 2, 0, 0),
                LocationId = 3
            };
            var expectedSession = new Session { Id = 5, Comment = "Test Session", GameId = 2 };

            _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ReturnsAsync(expectedSession);

            var result = await _sessionService.Create(session);

            result.Should().Be(expectedSession);
            _sessionRepositoryMock.Verify(x => x.CreateAsync(
                It.Is<Session>(s => s.Id == session.Id && s.Comment == session.Comment && s.GameId == session.GameId)), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldThrowException_WhenRepositoryThrows()
        {
            var session = new Session { Comment = "Test Session", GameId = 1 };
            var expectedException = new ArgumentException("Invalid session");

            _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<ArgumentException>(
                () => _sessionService.Create(session));

            exception.Should().Be(expectedException);
            _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldCallRepositoryDelete_WhenCalled()
        {
            const int sessionId = 1;

            _sessionRepositoryMock.Setup(x => x.DeleteAsync(sessionId)).ReturnsAsync(true);

            await _sessionService.Delete(sessionId);

            _sessionRepositoryMock.Verify(x => x.DeleteAsync(sessionId), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldThrowException_WhenRepositoryThrows()
        {
            const int sessionId = 1;
            var expectedException = new KeyNotFoundException("Session not found");

            _sessionRepositoryMock.Setup(x => x.DeleteAsync(sessionId)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<KeyNotFoundException>(
                () => _sessionService.Delete(sessionId));

            exception.Should().Be(expectedException);
            _sessionRepositoryMock.Verify(x => x.DeleteAsync(sessionId), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturnUpdatedSession_WhenRepositorySucceeds()
        {
            var inputSession = new Session { Id = 1, Comment = "Updated Session", GameId = 2 };
            var expectedSession = new Session { Id = 1, Comment = "Updated Session", GameId = 2 };

            _sessionRepositoryMock.Setup(x => x.UpdateAsync(inputSession)).ReturnsAsync(expectedSession);

            var result = await _sessionService.Update(inputSession);

            result.Should().NotBeNull();
            result.Should().Be(expectedSession);
            _sessionRepositoryMock.Verify(x => x.UpdateAsync(inputSession), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldPassCorrectSession_WhenCalled()
        {
            var session = new Session 
            { 
                Id = 5, 
                Comment = "Updated Comment", 
                GameId = 3,
                Start = new DateTime(2023, 6, 15),
                End = new DateTime(2023, 6, 15, 3, 30, 0),
                LocationId = 2
            };
            var expectedSession = new Session { Id = 5, Comment = "Updated Comment", GameId = 3 };

            _sessionRepositoryMock.Setup(x => x.UpdateAsync(session)).ReturnsAsync(expectedSession);

            var result = await _sessionService.Update(session);

            result.Should().Be(expectedSession);
            _sessionRepositoryMock.Verify(x => x.UpdateAsync(
                It.Is<Session>(s => s.Id == session.Id && s.Comment == session.Comment && s.GameId == session.GameId)), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldThrowException_WhenRepositoryThrows()
        {
            var session = new Session { Id = 1, Comment = "Test Session", GameId = 1 };
            var expectedException = new InvalidOperationException("Update failed");

            _sessionRepositoryMock.Setup(x => x.UpdateAsync(session)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _sessionService.Update(session));

            exception.Should().Be(expectedException);
            _sessionRepositoryMock.Verify(x => x.UpdateAsync(session), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ShouldReturnSession_WhenSessionExists()
        {
            const int sessionId = 1;
            var expectedSession = new Session 
            { 
                Id = sessionId, 
                Comment = "Test Session", 
                GameId = 2,
                Start = new DateTime(2023, 1, 1),
                End = new DateTime(2023, 1, 1, 1, 30, 0),
                LocationId = 1
            };

            _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync(expectedSession);

            var result = await _sessionService.Get(sessionId);

            result.Should().NotBeNull();
            result.Should().Be(expectedSession);
            _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ShouldReturnNull_WhenSessionDoesNotExist()
        {
            const int sessionId = 1;

            _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync((Session?)null);

            var result = await _sessionService.Get(sessionId);

            result.Should().BeNull();
            _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ShouldThrowException_WhenRepositoryThrows()
        {
            const int sessionId = 1;
            var expectedException = new TimeoutException("Database timeout");

            _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<TimeoutException>(
                () => _sessionService.Get(sessionId));

            exception.Should().Be(expectedException);
            _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task SessionService_ShouldPassThroughAllRepositoryResults_WhenCalled()
        {
            var createdSession = new Session { Id = 1, Comment = "Created" };
            var updatedSession = new Session { Id = 2, Comment = "Updated" };
            var retrievedSession = new Session { Id = 3, Comment = "Retrieved" };

            _sessionRepositoryMock.Setup(x => x.CreateAsync(It.IsAny<Session>())).ReturnsAsync(createdSession);
            _sessionRepositoryMock.Setup(x => x.UpdateAsync(It.IsAny<Session>())).ReturnsAsync(updatedSession);
            _sessionRepositoryMock.Setup(x => x.GetByIdAsync(It.IsAny<int>())).ReturnsAsync(retrievedSession);

            var createResult = await _sessionService.Create(new Session());
            var updateResult = await _sessionService.Update(new Session());
            var getResult = await _sessionService.Get(1);

            createResult.Should().BeSameAs(createdSession);
            updateResult.Should().BeSameAs(updatedSession);
            getResult.Should().BeSameAs(retrievedSession);
        }

        [Fact]
        public async Task SessionService_ShouldBeConsistent_WhenCalledMultipleTimes()
        {
            var session = new Session { Id = 1, Comment = "Test" };
            const int sessionId = 1;

            _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ReturnsAsync(session);
            _sessionRepositoryMock.Setup(x => x.GetByIdAsync(sessionId)).ReturnsAsync(session);

            var createResult1 = await _sessionService.Create(session);
            var createResult2 = await _sessionService.Create(session);
            var getResult1 = await _sessionService.Get(sessionId);
            var getResult2 = await _sessionService.Get(sessionId);

            createResult1.Should().BeSameAs(session);
            createResult2.Should().BeSameAs(session);
            getResult1.Should().BeSameAs(session);
            getResult2.Should().BeSameAs(session);

            _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Exactly(2));
            _sessionRepositoryMock.Verify(x => x.GetByIdAsync(sessionId), Times.Exactly(2));
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }

        [Theory]
        [InlineData(typeof(ArgumentException), "Invalid argument")]
        [InlineData(typeof(InvalidOperationException), "Invalid operation")]
        [InlineData(typeof(TimeoutException), "Request timeout")]
        public async Task SessionService_ShouldPropagateAllExceptionTypes_WhenRepositoryThrows(Type exceptionType, string message)
        {
            var expectedException = (Exception)Activator.CreateInstance(exceptionType, message);
            var session = new Session { Id = 1, Comment = "Test" };

            _sessionRepositoryMock.Setup(x => x.CreateAsync(session)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync(exceptionType, () => _sessionService.Create(session));

            exception.Should().Be(expectedException);
            exception.Message.Should().Be(message);
            _sessionRepositoryMock.Verify(x => x.CreateAsync(session), Times.Once);
            _sessionRepositoryMock.VerifyNoOtherCalls();
        }
    }