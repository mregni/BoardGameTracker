using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class PlayerServiceTests
    {
        private readonly Mock<IPlayerRepository> _playerRepositoryMock;
        private readonly Mock<IImageService> _imageServiceMock;
        private readonly PlayerService _playerService;

        public PlayerServiceTests()
        {
            _playerRepositoryMock = new Mock<IPlayerRepository>();
            _imageServiceMock = new Mock<IImageService>();
            _playerService = new PlayerService(_playerRepositoryMock.Object, _imageServiceMock.Object);
        }

        [Fact]
        public async Task GetList_ShouldReturnPlayerList_WhenRepositoryReturnsData()
        {
            var expectedPlayers = new List<Player>
            {
                new() { Id = 1, Name = "John", Image = "john.jpg" },
                new() { Id = 2, Name = "Jane", Image = "jane.jpg" }
            };

            _playerRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedPlayers);

            var result = await _playerService.GetList();

            result.Should().NotBeNull();
            result.Should().HaveCount(2);
            result.Should().BeEquivalentTo(expectedPlayers);
            _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetList_ShouldReturnEmptyList_WhenRepositoryReturnsEmptyList()
        {
            var expectedPlayers = new List<Player>();

            _playerRepositoryMock.Setup(x => x.GetAllAsync()).ReturnsAsync(expectedPlayers);

            var result = await _playerService.GetList();

            result.Should().NotBeNull();
            result.Should().BeEmpty();
            _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Create_ShouldReturnCreatedPlayer_WhenRepositorySucceeds()
        {
            var inputPlayer = new Player { Name = "New Player", Image = "new.jpg" };
            var expectedPlayer = new Player { Id = 1, Name = "New Player", Image = "new.jpg" };

            _playerRepositoryMock.Setup(x => x.CreateAsync(inputPlayer)).ReturnsAsync(expectedPlayer);

            var result = await _playerService.Create(inputPlayer);

            result.Should().NotBeNull();
            result.Should().Be(expectedPlayer);
            _playerRepositoryMock.Verify(x => x.CreateAsync(inputPlayer), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ShouldReturnPlayer_WhenPlayerExists()
        {
            const int playerId = 1;
            var expectedPlayer = new Player { Id = playerId, Name = "Test Player", Image = "test.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(playerId)).ReturnsAsync(expectedPlayer);

            var result = await _playerService.Get(playerId);

            result.Should().NotBeNull();
            result.Should().Be(expectedPlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Get_ShouldReturnNull_WhenPlayerDoesNotExist()
        {
            const int playerId = 1;

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(playerId)).ReturnsAsync((Player?)null);

            var result = await _playerService.Get(playerId);

            result.Should().BeNull();
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldUpdatePlayerAndDeleteOldImage_WhenImageHasChanged()
        {
            var updatePlayer = new Player { Id = 1, Name = "Updated Name", Image = "new-image.jpg" };
            var dbPlayer = new Player { Id = 1, Name = "Old Name", Image = "old-image.jpg" };
            var expectedUpdatedPlayer = new Player { Id = 1, Name = "Updated Name", Image = "new-image.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(updatePlayer.Id)).ReturnsAsync(dbPlayer);
            _playerRepositoryMock.Setup(x => x.UpdateAsync(dbPlayer)).ReturnsAsync(expectedUpdatedPlayer);

            var result = await _playerService.Update(updatePlayer);

            result.Should().Be(expectedUpdatedPlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(updatePlayer.Id), Times.Once);
            _playerRepositoryMock.Verify(x => x.UpdateAsync(dbPlayer), Times.Once);
            _imageServiceMock.Verify(x => x.DeleteImage("old-image.jpg"), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturnInputPlayer_WhenPlayerNotFoundInDatabase()
        {
            var updatePlayer = new Player { Id = 1, Name = "Updated Name", Image = "new-image.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(updatePlayer.Id)).ReturnsAsync((Player?)null);

            var result = await _playerService.Update(updatePlayer);

            result.Should().Be(updatePlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(updatePlayer.Id), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldReturnInputPlayer_WhenImageHasNotChanged()
        {
            var updatePlayer = new Player { Id = 1, Name = "Updated Name", Image = "same-image.jpg" };
            var dbPlayer = new Player { Id = 1, Name = "Old Name", Image = "same-image.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(updatePlayer.Id)).ReturnsAsync(dbPlayer);

            var result = await _playerService.Update(updatePlayer);

            result.Should().Be(updatePlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(updatePlayer.Id), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldHandleNullImages_WhenBothImagesAreNull()
        {
            var updatePlayer = new Player { Id = 1, Name = "Updated Name", Image = null };
            var dbPlayer = new Player { Id = 1, Name = "Old Name", Image = null };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(updatePlayer.Id)).ReturnsAsync(dbPlayer);

            var result = await _playerService.Update(updatePlayer);

            result.Should().Be(updatePlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(updatePlayer.Id), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldDeleteOldImage_WhenOldImageExistsAndNewImageIsNull()
        {
            var updatePlayer = new Player { Id = 1, Name = "Updated Name", Image = null };
            var dbPlayer = new Player { Id = 1, Name = "Old Name", Image = "old-image.jpg" };
            var expectedUpdatedPlayer = new Player { Id = 1, Name = "Updated Name", Image = null };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(updatePlayer.Id)).ReturnsAsync(dbPlayer);
            _playerRepositoryMock.Setup(x => x.UpdateAsync(dbPlayer)).ReturnsAsync(expectedUpdatedPlayer);

            var result = await _playerService.Update(updatePlayer);

            result.Should().Be(expectedUpdatedPlayer);
            _playerRepositoryMock.Verify(x => x.GetByIdAsync(updatePlayer.Id), Times.Once);
            _playerRepositoryMock.Verify(x => x.UpdateAsync(dbPlayer), Times.Once);
            _imageServiceMock.Verify(x => x.DeleteImage("old-image.jpg"), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task CountAsync_ShouldReturnCount_WhenRepositoryReturnsCount()
        {
            const int expectedCount = 42;

            _playerRepositoryMock.Setup(x => x.CountAsync()).ReturnsAsync(expectedCount);

            var result = await _playerService.CountAsync();

            result.Should().Be(expectedCount);
            _playerRepositoryMock.Verify(x => x.CountAsync(), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldDeletePlayerAndImage_WhenPlayerExists()
        {
            const int playerId = 1;
            var player = new Player { Id = playerId, Name = "Test Player", Image = "test.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(playerId)).ReturnsAsync(player);
            _playerRepositoryMock.Setup(x => x.DeleteAsync(playerId)).ReturnsAsync(true);

            await _playerService.Delete(playerId);

            _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.DeleteAsync(playerId), Times.Once);
            _imageServiceMock.Verify(x => x.DeleteImage("test.jpg"), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldDoNothing_WhenPlayerDoesNotExist()
        {
            const int playerId = 1;

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(playerId)).ReturnsAsync((Player?)null);

            await _playerService.Delete(playerId);

            _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Delete_ShouldHandleNullImage_WhenPlayerHasNoImage()
        {
            const int playerId = 1;
            var player = new Player { Id = playerId, Name = "Test Player", Image = null };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(playerId)).ReturnsAsync(player);
            _playerRepositoryMock.Setup(x => x.DeleteAsync(playerId)).ReturnsAsync(true);

            await _playerService.Delete(playerId);

            _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.DeleteAsync(playerId), Times.Once);
            _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetStats_ShouldReturnPlayerStatistics_WhenAllDataAvailable()
        {
            const int playerId = 1;
            const int expectedPlayCount = 10;
            const int expectedWinCount = 5;
            const double expectedTotalPlayedTime = 120.5;
            const int expectedDistinctGameCount = 3;
            var bestGame = new Game { Id = 1, Title = "Best Game", Image = "game.jpg" };
            const int expectedGameWins = 3;

            _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(expectedPlayCount);
            _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(expectedWinCount);
            _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(expectedTotalPlayedTime);
            _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(expectedDistinctGameCount);
            _playerRepositoryMock.Setup(x => x.GetBestGame(playerId)).ReturnsAsync(bestGame);
            _playerRepositoryMock.Setup(x => x.GetWinCount(playerId, bestGame.Id)).ReturnsAsync(expectedGameWins);

            var result = await _playerService.GetStats(playerId);

            result.Should().NotBeNull();
            result.PlayCount.Should().Be(expectedPlayCount);
            result.WinCount.Should().Be(expectedWinCount);
            result.TotalPlayedTime.Should().Be(expectedTotalPlayedTime);
            result.DistinctGameCount.Should().Be(expectedDistinctGameCount);
            result.MostWinsGame.Should().NotBeNull();
            result.MostWinsGame.Id.Should().Be(bestGame.Id);
            result.MostWinsGame.Title.Should().Be(bestGame.Title);
            result.MostWinsGame.Image.Should().Be(bestGame.Image);
            result.MostWinsGame.TotalWins.Should().Be(expectedGameWins);

            _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetDistinctGameCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetBestGame(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetWinCount(playerId, bestGame.Id), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetStats_ShouldReturnStatisticsWithoutBestGame_WhenNoBestGameFound()
        {
            const int playerId = 1;
            const int expectedPlayCount = 5;
            const int expectedWinCount = 2;
            const double expectedTotalPlayedTime = 60.0;
            const int expectedDistinctGameCount = 1;

            _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(expectedPlayCount);
            _playerRepositoryMock.Setup(x => x.GetTotalWinCount(playerId)).ReturnsAsync(expectedWinCount);
            _playerRepositoryMock.Setup(x => x.GetPlayLengthInMinutes(playerId)).ReturnsAsync(expectedTotalPlayedTime);
            _playerRepositoryMock.Setup(x => x.GetDistinctGameCount(playerId)).ReturnsAsync(expectedDistinctGameCount);
            _playerRepositoryMock.Setup(x => x.GetBestGame(playerId)).ReturnsAsync((Game?)null);

            var result = await _playerService.GetStats(playerId);

            result.Should().NotBeNull();
            result.PlayCount.Should().Be(expectedPlayCount);
            result.WinCount.Should().Be(expectedWinCount);
            result.TotalPlayedTime.Should().Be(expectedTotalPlayedTime);
            result.DistinctGameCount.Should().Be(expectedDistinctGameCount);
            result.MostWinsGame.Should().BeNull();

            _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetTotalWinCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetPlayLengthInMinutes(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetDistinctGameCount(playerId), Times.Once);
            _playerRepositoryMock.Verify(x => x.GetBestGame(playerId), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task GetTotalPlayCount_ShouldReturnPlayCount_WhenRepositoryReturnsCount()
        {
            const int playerId = 1;
            const int expectedPlayCount = 25;

            _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ReturnsAsync(expectedPlayCount);

            var result = await _playerService.GetTotalPlayCount(playerId);

            result.Should().Be(expectedPlayCount);
            _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
            _playerRepositoryMock.VerifyNoOtherCalls();
            _imageServiceMock.VerifyNoOtherCalls();
        }

        [Fact]
        public async Task Update_ShouldUpdateDbPlayerProperties_WhenImageChanged()
        {
            var updatePlayer = new Player { Id = 1, Name = "New Name", Image = "new.jpg" };
            var dbPlayer = new Player { Id = 1, Name = "Old Name", Image = "old.jpg" };
            var updatedPlayer = new Player { Id = 1, Name = "New Name", Image = "new.jpg" };

            _playerRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(dbPlayer);
            _playerRepositoryMock.Setup(x => x.UpdateAsync(dbPlayer)).ReturnsAsync(updatedPlayer);

            await _playerService.Update(updatePlayer);

            dbPlayer.Name.Should().Be("New Name");
            dbPlayer.Image.Should().Be("new.jpg");
            _playerRepositoryMock.Verify(x => x.UpdateAsync(dbPlayer), Times.Once);
            _imageServiceMock.Verify(x => x.DeleteImage("old.jpg"), Times.Once);
        }

        [Fact]
        public async Task GetStats_ShouldThrowException_WhenRepositoryThrows()
        {
            const int playerId = 1;
            var expectedException = new InvalidOperationException("Repository error");

            _playerRepositoryMock.Setup(x => x.GetTotalPlayCount(playerId)).ThrowsAsync(expectedException);

            var exception = await Assert.ThrowsAsync<InvalidOperationException>(
                () => _playerService.GetStats(playerId));

            exception.Should().Be(expectedException);
            _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
        }
    }