using System.Threading.Tasks;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Games.Factories;
using BoardGameTracker.Core.Games.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class BggImportServiceTests
{
    private readonly Mock<IBggApi> _bggApiMock;
    private readonly Mock<IBggGameTranslator> _bggGameTranslatorMock;
    private readonly Mock<IGameFactory> _gameFactoryMock;
    private readonly Mock<IGameRepository> _gameRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<ILogger<BggImportService>> _loggerMock;
    private readonly BggImportService _bggImportService;

    public BggImportServiceTests()
    {
        _bggApiMock = new Mock<IBggApi>();
        _bggGameTranslatorMock = new Mock<IBggGameTranslator>();
        _gameFactoryMock = new Mock<IGameFactory>();
        _gameRepositoryMock = new Mock<IGameRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _loggerMock = new Mock<ILogger<BggImportService>>();

        _bggImportService = new BggImportService(
            _bggApiMock.Object,
            _bggGameTranslatorMock.Object,
            _gameFactoryMock.Object,
            _gameRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _bggApiMock.VerifyNoOtherCalls();
        _bggGameTranslatorMock.VerifyNoOtherCalls();
        _gameFactoryMock.VerifyNoOtherCalls();
        _gameRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    #region GetGameByBggId Tests

    [Fact]
    public async Task GetGameByBggId_ShouldReturnGame_WhenGameExists()
    {
        // Arrange
        var bggId = 12345;
        var game = new Game("Test Game") { Id = 1 };
        game.UpdateBggId(bggId);

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(bggId))
            .ReturnsAsync(game);

        // Act
        var result = await _bggImportService.GetGameByBggId(bggId);

        // Assert
        result.Should().NotBeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetGameByBggId_ShouldReturnNull_WhenGameDoesNotExist()
    {
        // Arrange
        var bggId = 999;

        _gameRepositoryMock
            .Setup(x => x.GetGameByBggId(bggId))
            .ReturnsAsync((Game?)null);

        // Act
        var result = await _bggImportService.GetGameByBggId(bggId);

        // Assert
        result.Should().BeNull();

        _gameRepositoryMock.Verify(x => x.GetGameByBggId(bggId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion
}
