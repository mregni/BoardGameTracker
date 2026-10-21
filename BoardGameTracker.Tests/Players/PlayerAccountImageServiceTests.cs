using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Auth.Specifications;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Players.Specifications;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Players;

public class PlayerAccountImageServiceTests
{
    private const string AccountImage = "/images/profile/avatar.webp";
    private const string CopiedImage = "/images/profile/player_copy.webp";

    private readonly Mock<IPlayerRepository> _playerRepositoryMock = new();
    private readonly Mock<IReadRepository<ApplicationUser>> _userRepositoryMock = new();
    private readonly Mock<IImageService> _imageServiceMock = new();
    private readonly Mock<IUnitOfWork> _unitOfWorkMock = new();
    private readonly PlayerAccountImageService _service;

    public PlayerAccountImageServiceTests()
    {
        _imageServiceMock.Setup(x => x.CopyProfileImage(AccountImage)).ReturnsAsync(CopiedImage);
        _service = new PlayerAccountImageService(
            _playerRepositoryMock.Object,
            _userRepositoryMock.Object,
            _imageServiceMock.Object,
            _unitOfWorkMock.Object,
            Mock.Of<ILogger<PlayerAccountImageService>>());
    }

    private void VerifyNoOtherCalls()
    {
        _playerRepositoryMock.VerifyNoOtherCalls();
        _userRepositoryMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
    }

    private Player SetupPlayer(int id, string? image = null)
    {
        var player = new Player("Jane", image) { Id = id };
        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<PlayerByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(player);
        return player;
    }

    private void VerifyPlayerLoaded(int times = 1)
    {
        _playerRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.IsAny<PlayerByIdForUpdateSpec>(), It.IsAny<CancellationToken>()),
            Times.Exactly(times));
    }

    private static ApplicationUser LinkedUser(int playerId, string? image = AccountImage)
    {
        var user = new ApplicationUser("jane", "jane@example.com", "Jane");
        user.LinkPlayer(playerId);
        if (image != null)
        {
            user.UseUploadedProfileImage(image);
        }

        return user;
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldCopyTheAccountPicture_WhenThePlayerHasNoImage()
    {
        var player = SetupPlayer(4);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeTrue();
        player.Image.Should().Be(CopiedImage);
        player.ImageSource.Should().Be(PlayerImageSource.Account);
        VerifyPlayerLoaded();
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldReplaceAndDeleteThePreviousAccountCopy()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");

        await _service.ApplyAccountImageAsync(LinkedUser(4));

        player.Image.Should().Be(CopiedImage);
        VerifyPlayerLoaded();
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldLeaveAnUploadedPlayerImageAlone()
    {
        var player = SetupPlayer(4, "/images/profile/own.webp");

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeFalse();
        player.Image.Should().Be("/images/profile/own.webp");
        player.ImageSource.Should().Be(PlayerImageSource.Upload);
        VerifyPlayerLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldClearTheAccountCopy_WhenTheAccountPictureWasRemoved()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4, null));

        applied.Should().BeTrue();
        player.Image.Should().BeNull();
        player.ImageSource.Should().Be(PlayerImageSource.None);
        VerifyPlayerLoaded();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDoNothing_WhenNeitherHasAPicture()
    {
        SetupPlayer(4);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4, null));

        applied.Should().BeFalse();
        VerifyPlayerLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDoNothing_WhenTheAccountIsNotLinked()
    {
        var applied = await _service.ApplyAccountImageAsync(new ApplicationUser("jane", null));

        applied.Should().BeFalse();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDoNothing_WhenThePlayerIsGone()
    {
        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.IsAny<PlayerByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((Player?)null);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeFalse();
        VerifyPlayerLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldKeepThePlayer_WhenTheAccountFileCannotBeCopied()
    {
        var player = SetupPlayer(4);
        _imageServiceMock.Setup(x => x.CopyProfileImage(AccountImage)).ReturnsAsync((string?)null);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeFalse();
        player.ImageSource.Should().Be(PlayerImageSource.None);
        VerifyPlayerLoaded();
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDeleteTheNewCopy_WhenSavingFails()
    {
        SetupPlayer(4);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _service.ApplyAccountImageAsync(LinkedUser(4));

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyPlayerLoaded();
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(CopiedImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldClearAnAccountSourcedImage()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");

        await _service.ReleaseAsync(4);

        player.Image.Should().BeNull();
        player.ImageSource.Should().Be(PlayerImageSource.None);
        VerifyPlayerLoaded();
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldKeepAnUploadedImage()
    {
        var player = SetupPlayer(4, "/images/profile/own.webp");

        await _service.ReleaseAsync(4);

        player.Image.Should().Be("/images/profile/own.webp");
        VerifyPlayerLoaded();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDoNothing_WithoutAPlayer()
    {
        await _service.ReleaseAsync(null);

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldClearTheOldPlayer_AndApplyToTheNewOne()
    {
        var oldPlayer = new Player("Old") { Id = 1 };
        oldPlayer.UseAccountImage("/images/profile/old_copy.webp");
        var newPlayer = new Player("New") { Id = 2 };
        _playerRepositoryMock
            .SetupSequence(x => x.SingleOrDefaultAsync(It.IsAny<PlayerByIdForUpdateSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(oldPlayer)
            .ReturnsAsync(newPlayer);

        await _service.RelinkAsync(1, LinkedUser(2));

        oldPlayer.Image.Should().BeNull();
        oldPlayer.ImageSource.Should().Be(PlayerImageSource.None);
        newPlayer.Image.Should().Be(CopiedImage);
        newPlayer.ImageSource.Should().Be(PlayerImageSource.Account);
        VerifyPlayerLoaded(2);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldDoNothing_WhenTheLinkDidNotChange()
    {
        await _service.RelinkAsync(2, LinkedUser(2));

        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyLinkedAccountImageAsync_ShouldCopyTheLinkedAccountPicture()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LinkedAccountByPlayerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new PlayerLinkedAccountDto { Id = "u1", Username = "jane", ProfileImage = AccountImage });

        var result = await _service.CopyLinkedAccountImageAsync(4);

        result.Should().Be(CopiedImage);
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LinkedAccountByPlayerIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyLinkedAccountImageAsync_ShouldReturnNull_WhenNoAccountPictureExists()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.IsAny<LinkedAccountByPlayerIdSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync((PlayerLinkedAccountDto?)null);

        var result = await _service.CopyLinkedAccountImageAsync(4);

        result.Should().BeNull();
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.IsAny<LinkedAccountByPlayerIdSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackfillAsync_ShouldGiveEveryWaitingPlayerTheAccountPicture()
    {
        var player = SetupPlayer(4);
        _userRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([LinkedUser(4)]);

        var applied = await _service.BackfillAsync();

        applied.Should().Be(1);
        player.Image.Should().Be(CopiedImage);
        player.ImageSource.Should().Be(PlayerImageSource.Account);
        _userRepositoryMock.Verify(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyPlayerLoaded();
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackfillAsync_ShouldReturnZero_WhenNothingWaits()
    {
        _userRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        var applied = await _service.BackfillAsync();

        applied.Should().Be(0);
        _userRepositoryMock.Verify(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }
}
