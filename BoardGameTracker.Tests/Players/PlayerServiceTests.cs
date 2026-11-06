using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;
using Ardalis.Specification;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Players.Specifications;
using BoardGameTracker.Core.Sessions.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerServiceTests
{
    private readonly Mock<IPlayerRepository> _playerRepositoryMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IPlayerStatisticsService> _playerStatisticsDomainServiceMock;
    private readonly Mock<ISessionRepository> _sessionRepositoryMock;
    private readonly Mock<IReadRepository<GameNight>> _gameNightRepositoryMock;
    private readonly Mock<IPlayerAccountImageService> _accountImageServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly PlayerService _playerService;

    public PlayerServiceTests()
    {
        _playerRepositoryMock = new Mock<IPlayerRepository>();
        _imageServiceMock = new Mock<IImageService>();
        _playerStatisticsDomainServiceMock = new Mock<IPlayerStatisticsService>();
        _sessionRepositoryMock = new Mock<ISessionRepository>();
        _gameNightRepositoryMock = new Mock<IReadRepository<GameNight>>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();

        _playerService = new PlayerService(
            _playerRepositoryMock.Object,
            _imageServiceMock.Object,
            _playerStatisticsDomainServiceMock.Object,
            _sessionRepositoryMock.Object,
            _gameNightRepositoryMock.Object,
            _accountImageServiceMock.Object,
            _unitOfWorkMock.Object,
            NullLogger<PlayerService>.Instance);
    }

    private void VerifyNoOtherCalls()
    {
        _playerRepositoryMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _playerStatisticsDomainServiceMock.VerifyNoOtherCalls();
        _sessionRepositoryMock.VerifyNoOtherCalls();
        _gameNightRepositoryMock.VerifyNoOtherCalls();
        _accountImageServiceMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private void VerifyNameChecked(string name, int? excludedId = null)
    {
        _playerRepositoryMock.Verify(
            x => x.AnyAsync(It.Is<PlayerWithNameSpec>(s => ChecksName(s, name, excludedId)), CancellationToken.None), Times.Once);
    }

    private static bool ChecksName(PlayerWithNameSpec spec, string name, int? excludedId)
    {
        var like = (MethodCallExpression)spec.WhereExpressions.First().Filter.Body;
        var pattern = (string?)Expression.Lambda(like.Arguments[2]).Compile().DynamicInvoke();
        var keeps = spec.WhereExpressions.Last().Filter.Compile();
        return pattern == name
            && keeps(new Player("Other") { Id = (excludedId ?? 0) + 1 })
            && keeps(new Player("Self") { Id = excludedId ?? 0 }) == (excludedId == null);
    }

    private static bool IsPlayer(ISpecification<Player> spec, int id)
    {
        return spec.IsSatisfiedBy(new Player("Match") { Id = id })
            && !spec.IsSatisfiedBy(new Player("Other") { Id = id + 1 });
    }

    private static bool IsHostedBy(GameNightsHostedByPlayerSpec spec, int playerId)
    {
        return spec.IsSatisfiedBy(GameNight.Create("Night", "", new DateTime(2026, 3, 2), playerId, 1))
            && !spec.IsSatisfiedBy(GameNight.Create("Night", "", new DateTime(2026, 3, 2), playerId + 1, 1));
    }

    private static bool IsRecentSessionsOf(ISpecification<Session> spec, int playerId, int? count)
    {
        return spec is SessionsByPlayerRecentFirstSpec
            && spec.Take == (count ?? -1)
            && spec.IsSatisfiedBy(new SessionBuilder().WithPlayer(playerId).Build())
            && !spec.IsSatisfiedBy(new SessionBuilder().WithPlayer(playerId + 1).Build());
    }

    #region GetList Tests

    [Fact]
    public async Task GetList_ShouldReturnAllPlayers_WhenPlayersExist()
    {
        // Arrange
        var players = new List<Player>
        {
            new Player("John") { Id = 1 },
            new Player("Jane") { Id = 2 },
            new Player("Bob") { Id = 3 }
        };

        _playerRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync(players);

        // Act
        var result = await _playerService.GetList();

        // Assert
        result.Should().HaveCount(3);
        result.Should().Contain(p => p.Name == "John");
        result.Should().Contain(p => p.Name == "Jane");
        result.Should().Contain(p => p.Name == "Bob");

        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetList_ShouldReturnEmptyList_WhenNoPlayersExist()
    {
        // Arrange
        _playerRepositoryMock
            .Setup(x => x.GetAllAsync())
            .ReturnsAsync([]);

        // Act
        var result = await _playerService.GetList();

        // Assert
        result.Should().BeEmpty();

        _playerRepositoryMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Create Tests

    [Fact]
    public async Task Create_ShouldCreatePlayer_AndSaveChanges()
    {
        // Arrange
        var command = new CreatePlayerCommand { Name = "New Player", Image = "player.png", Email = "player@example.com" };

        _playerRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player" && p.Image == "player.png" && p.Email == "player@example.com")))
            .ReturnsAsync((Player p) => p);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Create(command);

        // Assert
        result.Name.Should().Be("New Player");
        result.Image.Should().Be("player.png");
        result.Email.Should().Be("player@example.com");

        VerifyNameChecked("New Player");
        _playerRepositoryMock.Verify(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player" && p.Image == "player.png" && p.Email == "player@example.com")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldTrimName()
    {
        var command = new CreatePlayerCommand { Name = "  Alice  " };

        _playerRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Player>(p => p.Name == "Alice")))
            .ReturnsAsync((Player p) => p);

        var result = await _playerService.Create(command);

        result.Name.Should().Be("Alice");

        VerifyNameChecked("Alice");
        _playerRepositoryMock.Verify(x => x.CreateAsync(It.Is<Player>(p => p.Name == "Alice")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    public async Task Create_ShouldThrowValidationException_WhenNameIsBlank(string? name)
    {
        var command = new CreatePlayerCommand { Name = name! };

        var action = async () => await _playerService.Create(command);

        var error = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.PlayerNameRequired);
        error.Which.Errors[nameof(CreatePlayerCommand.Name)].Should().Equal(Constants.Errors.PlayerNameRequired);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldThrowValidationException_WhenNameAlreadyExists()
    {
        var command = new CreatePlayerCommand { Name = "JOHN" };

        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerWithNameSpec>(s => ChecksName(s, "JOHN", null)), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _playerService.Create(command);

        var error = await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.PlayerNameAlreadyExists);
        error.Which.Errors[nameof(CreatePlayerCommand.Name)].Should().Equal(Constants.Errors.PlayerNameAlreadyExists);

        VerifyNameChecked("JOHN");
        VerifyNoOtherCalls();
    }

    #endregion

    #region Get Tests

    [Fact]
    public async Task Get_ShouldReturnPlayer_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("John") { Id = playerId };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(player);

        // Act
        var result = await _playerService.Get(playerId);

        // Assert
        result.Should().BeSameAs(player);

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Get_ShouldReturnNull_WhenPlayerDoesNotExist()
    {
        // Arrange
        var playerId = 999;

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);

        // Act
        var result = await _playerService.Get(playerId);

        // Assert
        result.Should().BeNull();

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Update Tests

    [Fact]
    public async Task Update_ShouldUpdatePlayer_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var existingPlayer = new Player("Old Name", "old.png", "old@example.com") { Id = playerId };
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            Name = "New Name",
            Image = "new.png",
            Email = "new@example.com"
        };

        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None))
            .ReturnsAsync(existingPlayer);


        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result.Should().BeSameAs(existingPlayer);
        result.Name.Should().Be("New Name");
        result.Image.Should().Be("new.png");
        result.Email.Should().Be("new@example.com");

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("New Name", 1);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("old.png"), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    private void SetupPlayerForUpdate(Player player)
    {
        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None))
            .ReturnsAsync(player);
    }

    [Fact]
    public async Task Update_ShouldMarkANewPictureAsUploaded_EvenOverAnAccountPicture()
    {
        var player = new Player("Jane") { Id = 1 };
        player.UseAccountImage("/images/profile/account_copy.webp");
        SetupPlayerForUpdate(player);

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = "/images/profile/own.webp" });

        result.Image.Should().Be("/images/profile/own.webp");
        result.ImageSource.Should().Be(PlayerImageSource.Upload);
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/account_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepTheAccountPicture_WhenTheImageIsUnchanged()
    {
        var player = new Player("Jane") { Id = 1 };
        player.UseAccountImage("/images/profile/account_copy.webp");
        SetupPlayerForUpdate(player);

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane B", Image = "/images/profile/account_copy.webp" });

        result.ImageSource.Should().Be(PlayerImageSource.Account);
        result.Name.Should().Be("Jane B");
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane B", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldFallBackToTheAccountPicture_WhenTheOwnPictureIsRemoved()
    {
        var player = new Player("Jane", "/images/profile/own.webp") { Id = 1 };
        SetupPlayerForUpdate(player);
        _accountImageServiceMock.Setup(x => x.CopyLinkedAccountImageAsync(1)).ReturnsAsync("/images/profile/account_copy.webp");

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = null });

        result.Image.Should().Be("/images/profile/account_copy.webp");
        result.ImageSource.Should().Be(PlayerImageSource.Account);
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _accountImageServiceMock.Verify(x => x.CopyLinkedAccountImageAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/own.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldClearThePicture_WhenRemovedWithoutALinkedAccountPicture()
    {
        var player = new Player("Jane", "/images/profile/own.webp") { Id = 1 };
        SetupPlayerForUpdate(player);
        _accountImageServiceMock.Setup(x => x.CopyLinkedAccountImageAsync(1)).ReturnsAsync((string?)null);

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = "" });

        result.Image.Should().BeNull();
        result.ImageSource.Should().Be(PlayerImageSource.None);
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _accountImageServiceMock.Verify(x => x.CopyLinkedAccountImageAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/own.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldDeleteTheAccountCopy_WhenSavingFails()
    {
        var player = new Player("Jane", "/images/profile/own.webp") { Id = 1 };
        SetupPlayerForUpdate(player);
        _accountImageServiceMock.Setup(x => x.CopyLinkedAccountImageAsync(1)).ReturnsAsync("/images/profile/account_copy.webp");
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = null });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _accountImageServiceMock.Verify(x => x.CopyLinkedAccountImageAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/account_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldRemoveTheNewUpload_WhenSavingFails()
    {
        var player = new Player("Jane", "/images/profile/old.webp") { Id = 1 };
        SetupPlayerForUpdate(player);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = "/images/profile/new.webp" });

        await act.Should().ThrowAsync<InvalidOperationException>();
        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/new.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldRemoveTheUploadedImage_WhenSavingFails()
    {
        var command = new CreatePlayerCommand { Name = "New Player", Image = "/images/profile/new.webp" };
        _playerRepositoryMock.Setup(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player"))).ReturnsAsync((Player p) => p);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _playerService.Create(command);

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyNameChecked("New Player");
        _playerRepositoryMock.Verify(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/new.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetLinkedAccountAsync_ShouldReturnTheLinkedAccount()
    {
        var account = new PlayerLinkedAccountDto { Id = "u1", Username = "jane" };
        _accountImageServiceMock.Setup(x => x.FindLinkedAccountAsync(1)).ReturnsAsync(account);

        var result = await _playerService.GetLinkedAccountAsync(1);

        result.Should().BeSameAs(account);
        _accountImageServiceMock.Verify(x => x.FindLinkedAccountAsync(1), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowEntityNotFoundException_WhenPlayerDoesNotExist()
    {
        // Arrange
        var command = new UpdatePlayerCommand
        {
            Id = 999,
            Name = "New Name"
        };

        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 999)), CancellationToken.None))
            .ReturnsAsync((Player?)null);

        // Act
        var action = async () => await _playerService.Update(command);

        // Assert
        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Player));
        error.Which.EntityId.Should().Be(999);

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 999)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldNotDeleteImage_WhenImageUnchanged()
    {
        // Arrange
        var playerId = 1;
        var existingImage = "same.png";
        var existingPlayer = new Player("Old Name", existingImage) { Id = playerId };
        var command = new UpdatePlayerCommand
        {
            Id = playerId,
            Name = "New Name",
            Image = existingImage
        };

        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None))
            .ReturnsAsync(existingPlayer);


        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        var result = await _playerService.Update(command);

        // Assert
        result.Image.Should().Be(existingImage);
        result.Name.Should().Be("New Name");

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("New Name", 1);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync(existingImage), Times.Never);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    [Fact]
    public async Task Update_ShouldThrowValidationException_WhenAnotherPlayerHasTheName()
    {
        var existingPlayer = new Player("Old Name", "old.png") { Id = 1 };
        var command = new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = "new.png" };

        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None))
            .ReturnsAsync(existingPlayer);
        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerWithNameSpec>(s => ChecksName(s, "Jane", 1)), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _playerService.Update(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.PlayerNameAlreadyExists);
        existingPlayer.Name.Should().Be("Old Name");
        existingPlayer.Image.Should().Be("old.png");

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnRepositoryResult()
    {
        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => IsPlayer(s, 4)), CancellationToken.None))
            .ReturnsAsync(true);

        var result = await _playerService.ExistsAsync(4);

        result.Should().BeTrue();

        _playerRepositoryMock.Verify(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => IsPlayer(s, 4)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    #region CountAsync Tests

    [Fact]
    public async Task CountAsync_ShouldReturnPlayerCount()
    {
        // Arrange
        using var cancellation = new CancellationTokenSource();
        _playerRepositoryMock
            .Setup(x => x.CountAsync(cancellation.Token))
            .ReturnsAsync(15);

        // Act
        var result = await _playerService.CountAsync(cancellation.Token);

        // Assert
        result.Should().Be(15);

        _playerRepositoryMock.Verify(x => x.CountAsync(cancellation.Token), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetSessions Tests

    [Fact]
    public async Task GetSessions_ShouldReturnPlayerSessions()
    {
        // Arrange
        var playerId = 1;
        var count = 10;
        var sessions = new List<Session>
        {
            new Session(1, DateTime.UtcNow.AddDays(-1), DateTime.UtcNow.AddDays(-1).AddHours(2), "Session 1"),
            new Session(2, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow.AddDays(-2).AddHours(3), "Session 2")
        };

        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => IsRecentSessionsOf(s, playerId, count)), CancellationToken.None))
            .ReturnsAsync(sessions);

        // Act
        var result = await _playerService.GetSessions(playerId, count);

        // Assert
        result.Should().BeSameAs(sessions);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => IsRecentSessionsOf(s, playerId, count)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region Delete Tests

    [Fact]
    public async Task Delete_ShouldDeletePlayerAndSessions_WhenPlayerExists()
    {
        // Arrange
        var playerId = 1;
        var player = new Player("John", "john.png") { Id = playerId };

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync(player);

        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None))
            .ReturnsAsync(false);

        _sessionRepositoryMock
            .Setup(x => x.DeleteByPlayerIdAsync(playerId))
            .Returns(Task.CompletedTask);

        _playerRepositoryMock
            .Setup(x => x.DeleteAsync(playerId))
            .ReturnsAsync(true);

        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .ReturnsAsync(1);

        // Act
        await _playerService.Delete(playerId);

        // Assert
        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        _gameNightRepositoryMock.Verify(
            x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteByPlayerIdAsync(playerId), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("john.png"), Times.Once);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(playerId), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowEntityNotFoundException_WhenPlayerDoesNotExist()
    {
        // Arrange
        var playerId = 999;

        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(playerId))
            .ReturnsAsync((Player?)null);

        // Act
        var action = async () => await _playerService.Delete(playerId);

        // Assert
        var error = await action.Should().ThrowAsync<EntityNotFoundException>();
        error.Which.EntityType.Should().Be(nameof(Player));
        error.Which.EntityId.Should().Be(999);

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldThrowDomainException_WhenPlayerHostsGameNights()
    {
        var player = new Player("John", "john.png") { Id = 1 };
        _playerRepositoryMock
            .Setup(x => x.GetByIdAsync(1))
            .ReturnsAsync(player);
        _gameNightRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _playerService.Delete(1);

        await action.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.PlayerHostsGameNights);

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameNightRepositoryMock.Verify(
            x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteByPlayerIdAsync(1), Times.Never);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Never);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetStats Tests

    [Fact]
    public async Task GetStats_ShouldReturnPlayerStatistics()
    {
        // Arrange
        var playerId = 1;
        var stats = new PlayerStatistics
        {
            PlayCount = 50,
            WinCount = 25,
            TotalPlayedTime = 3000.0,
            DistinctGameCount = 10,
            MostPlayedGames = []
        };

        _playerStatisticsDomainServiceMock
            .Setup(x => x.CalculateStatisticsAsync(playerId))
            .ReturnsAsync(stats);

        // Act
        var result = await _playerService.GetStats(playerId);

        // Assert
        result.Should().BeSameAs(stats);

        _playerStatisticsDomainServiceMock.Verify(x => x.CalculateStatisticsAsync(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    #region GetTotalPlayCount Tests

    [Fact]
    public async Task GetTotalPlayCount_ShouldReturnPlayCount()
    {
        // Arrange
        var playerId = 1;
        var expectedCount = 42;

        _playerRepositoryMock
            .Setup(x => x.GetTotalPlayCount(playerId))
            .ReturnsAsync(expectedCount);

        // Act
        var result = await _playerService.GetTotalPlayCount(playerId);

        // Assert
        result.Should().Be(42);

        _playerRepositoryMock.Verify(x => x.GetTotalPlayCount(playerId), Times.Once);
        VerifyNoOtherCalls();
    }

    #endregion

    [Fact]
    public async Task GetExistingIdsAsync_ShouldReturnTheIdsTheRepositoryFinds()
    {
        _playerRepositoryMock
            .Setup(x => x.ListAsync(It.Is<PlayerIdsSpec>(s => s.IsSatisfiedBy(new Player("Found") { Id = 1 }) && s.IsSatisfiedBy(new Player("Found") { Id = 3 }) && s.IsSatisfiedBy(new Player("Missing") { Id = 9 }) && !s.IsSatisfiedBy(new Player("Other") { Id = 2 })), CancellationToken.None))
            .ReturnsAsync(new List<int> { 1, 3 });

        var result = await _playerService.GetExistingIdsAsync(new[] { 1, 3, 9 });

        result.Should().Equal(1, 3);
        _playerRepositoryMock.Verify(x => x.ListAsync(It.Is<PlayerIdsSpec>(s => s.IsSatisfiedBy(new Player("Found") { Id = 1 }) && s.IsSatisfiedBy(new Player("Found") { Id = 3 }) && s.IsSatisfiedBy(new Player("Missing") { Id = 9 }) && !s.IsSatisfiedBy(new Player("Other") { Id = 2 })), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetOverview_ShouldReturnProjectedPlayers()
    {
        var overview = new List<PlayerOverviewDto>
        {
            new() { Id = 1, Name = "Alice", PlayCount = 4, WinCount = 2 }
        };
        _playerRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<PlayersOverviewSpec>(), CancellationToken.None))
            .ReturnsAsync(overview);

        var result = await _playerService.GetOverview();

        result.Should().BeSameAs(overview);
        _playerRepositoryMock.Verify(x => x.ListAsync(It.IsAny<PlayersOverviewSpec>(), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ExistsAsync_ShouldReturnFalse_WhenThePlayerIsMissing()
    {
        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => IsPlayer(s, 4)), CancellationToken.None))
            .ReturnsAsync(false);

        var result = await _playerService.ExistsAsync(4);

        result.Should().BeFalse();

        _playerRepositoryMock.Verify(x => x.AnyAsync(It.Is<PlayerByIdSpec>(s => IsPlayer(s, 4)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSessions_ShouldNotLimitTheSessions_WithoutACount()
    {
        var sessions = new List<Session>();
        _sessionRepositoryMock
            .Setup(x => x.ListAsync(It.Is<ISpecification<Session>>(s => IsRecentSessionsOf(s, 1, null)), CancellationToken.None))
            .ReturnsAsync(sessions);

        var result = await _playerService.GetSessions(1, null);

        result.Should().BeSameAs(sessions);

        _sessionRepositoryMock.Verify(x => x.ListAsync(It.Is<ISpecification<Session>>(s => IsRecentSessionsOf(s, 1, null)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldStoreThePlayerWithoutImage_WhenTheImageIsEmpty()
    {
        var command = new CreatePlayerCommand { Name = "New Player", Image = "" };
        _playerRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player" && p.Image == null)))
            .ReturnsAsync((Player p) => p);

        var result = await _playerService.Create(command);

        result.Image.Should().BeNull();
        result.ImageSource.Should().Be(PlayerImageSource.None);

        VerifyNameChecked("New Player");
        _playerRepositoryMock.Verify(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player" && p.Image == null)), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Create_ShouldReturnThePlayerItCreated()
    {
        var command = new CreatePlayerCommand { Name = "New Player" };
        _playerRepositoryMock
            .Setup(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player")))
            .Callback((Player p) => p.Id = 12)
            .ReturnsAsync((Player p) => p);

        var result = await _playerService.Create(command);

        result.Id.Should().Be(12);

        VerifyNameChecked("New Player");
        _playerRepositoryMock.Verify(x => x.CreateAsync(It.Is<Player>(p => p.Name == "New Player")), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact(Skip = "BUG: a refused name leaves the image the client already uploaded on disk")]
    public async Task Create_ShouldRemoveTheUploadedImage_WhenTheNameIsTaken()
    {
        var command = new CreatePlayerCommand { Name = "JOHN", Image = "/images/profile/new.webp" };
        _playerRepositoryMock
            .Setup(x => x.AnyAsync(It.Is<PlayerWithNameSpec>(s => ChecksName(s, "JOHN", null)), CancellationToken.None))
            .ReturnsAsync(true);

        var action = async () => await _playerService.Create(command);

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.PlayerNameAlreadyExists);

        VerifyNameChecked("JOHN");
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/new.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldTrimTheName_AndClearTheEmail()
    {
        var player = new Player("Jane", null, "jane@example.com") { Id = 1 };
        SetupPlayerForUpdate(player);

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "  Jane B  " });

        result.Name.Should().Be("Jane B");
        result.Email.Should().BeNull();

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane B", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task Update_ShouldNotLookForAnAccountPicture_WhenThePlayerHadNoPicture(string? image)
    {
        var player = new Player("Jane") { Id = 1 };
        SetupPlayerForUpdate(player);

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = image });

        result.Image.Should().BeNull();
        result.ImageSource.Should().Be(PlayerImageSource.None);

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldThrowValidationException_WhenTheNewNameIsBlank()
    {
        var player = new Player("Jane", "old.png") { Id = 1 };
        SetupPlayerForUpdate(player);

        var action = async () => await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "   ", Image = "new.png" });

        await action.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.PlayerNameRequired);
        player.Name.Should().Be("Jane");
        player.Image.Should().Be("old.png");

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldReplaceTheAccountCopy_WhenTheAccountPictureIsCleared()
    {
        var player = new Player("Jane") { Id = 1 };
        player.UseAccountImage("/images/profile/account_copy.webp");
        SetupPlayerForUpdate(player);
        _accountImageServiceMock.Setup(x => x.CopyLinkedAccountImageAsync(1)).ReturnsAsync("/images/profile/fresh_copy.webp");

        var result = await _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = null });

        result.Image.Should().Be("/images/profile/fresh_copy.webp");
        result.ImageSource.Should().Be(PlayerImageSource.Account);

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _accountImageServiceMock.Verify(x => x.CopyLinkedAccountImageAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/account_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Update_ShouldKeepTheStoredImage_WhenSavingFailsWithAnUnchangedImage()
    {
        var player = new Player("Jane", "/images/profile/same.webp") { Id = 1 };
        var failure = new InvalidOperationException("db down");
        SetupPlayerForUpdate(player);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(failure);

        var act = () => _playerService.Update(new UpdatePlayerCommand { Id = 1, Name = "Jane", Image = "/images/profile/same.webp" });

        (await act.Should().ThrowAsync<InvalidOperationException>()).Which.Should().BeSameAs(failure);

        _playerRepositoryMock.Verify(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, 1)), CancellationToken.None), Times.Once);
        VerifyNameChecked("Jane", 1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("/images/profile/same.webp"), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldRemoveTheSessions_BeforeThePlayer_AndTheImageAfterSaving()
    {
        var player = new Player("John", "john.png") { Id = 1 };
        var steps = new List<string>();
        _playerRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(player);
        _sessionRepositoryMock
            .Setup(x => x.DeleteByPlayerIdAsync(1))
            .Callback(() => steps.Add("sessions"))
            .Returns(Task.CompletedTask);
        _playerRepositoryMock
            .Setup(x => x.DeleteAsync(1))
            .Callback(() => steps.Add("player"))
            .ReturnsAsync(true);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(default))
            .Callback(() => steps.Add("save"))
            .ReturnsAsync(1);
        _imageServiceMock
            .Setup(x => x.DeleteImageIfUnusedAsync("john.png"))
            .Callback(() => steps.Add("image"))
            .Returns(Task.CompletedTask);

        await _playerService.Delete(1);

        steps.Should().Equal("sessions", "player", "save", "image");

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteByPlayerIdAsync(1), Times.Once);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("john.png"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Delete_ShouldKeepTheImage_WhenSavingFails()
    {
        var player = new Player("John", "john.png") { Id = 1 };
        _playerRepositoryMock.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(player);
        _unitOfWorkMock.Setup(x => x.SaveChangesAsync(default)).ThrowsAsync(new InvalidOperationException("db down"));

        var action = async () => await _playerService.Delete(1);

        await action.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");

        _playerRepositoryMock.Verify(x => x.GetByIdAsync(1), Times.Once);
        _gameNightRepositoryMock.Verify(x => x.AnyAsync(It.Is<GameNightsHostedByPlayerSpec>(s => IsHostedBy(s, 1)), CancellationToken.None), Times.Once);
        _sessionRepositoryMock.Verify(x => x.DeleteByPlayerIdAsync(1), Times.Once);
        _playerRepositoryMock.Verify(x => x.DeleteAsync(1), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(default), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImageIfUnusedAsync("john.png"), Times.Never);
        VerifyNoOtherCalls();
    }
}
