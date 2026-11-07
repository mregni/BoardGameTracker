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
using Microsoft.Extensions.Logging.Abstractions;
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
            NullLogger<PlayerAccountImageService>.Instance);
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
        SetupPlayerLookup(id, player);
        return player;
    }

    private void SetupPlayerLookup(int id, Player? player)
    {
        _playerRepositoryMock
            .Setup(x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, id)), CancellationToken.None))
            .ReturnsAsync(player);
    }

    private void VerifyPlayerLoaded(int id)
    {
        _playerRepositoryMock.Verify(
            x => x.SingleOrDefaultAsync(It.Is<PlayerByIdForUpdateSpec>(s => IsPlayer(s, id)), CancellationToken.None),
            Times.Once);
    }

    private static bool IsPlayer(PlayerByIdForUpdateSpec spec, int id)
    {
        return spec.IsSatisfiedBy(new Player("Match") { Id = id })
            && !spec.IsSatisfiedBy(new Player("Other") { Id = id + 1 });
    }

    private static bool IsLinkedTo(LinkedAccountByPlayerIdSpec spec, int playerId)
    {
        return spec.IsSatisfiedBy(LinkedUser(playerId)) && !spec.IsSatisfiedBy(LinkedUser(playerId + 1));
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
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
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
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
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
        VerifyPlayerLoaded(4);
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
        VerifyPlayerLoaded(4);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDoNothing_WhenNeitherHasAPicture()
    {
        SetupPlayer(4);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4, null));

        applied.Should().BeFalse();
        VerifyPlayerLoaded(4);
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
        SetupPlayerLookup(4, null);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeFalse();
        VerifyPlayerLoaded(4);
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
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldDeleteTheNewCopy_WhenSavingFails()
    {
        SetupPlayer(4);
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _service.ApplyAccountImageAsync(LinkedUser(4));

        await act.Should().ThrowAsync<InvalidOperationException>();
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
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
        VerifyPlayerLoaded(4);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldKeepAnUploadedImage()
    {
        var player = SetupPlayer(4, "/images/profile/own.webp");

        await _service.ReleaseAsync(4);

        player.Image.Should().Be("/images/profile/own.webp");
        VerifyPlayerLoaded(4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDoNothing_WithoutAPlayer()
    {
        await _service.ReleaseAsync(null);

        _playerRepositoryMock.Invocations.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldClearTheOldPlayer_AndApplyToTheNewOne()
    {
        var oldPlayer = new Player("Old") { Id = 1 };
        oldPlayer.UseAccountImage("/images/profile/old_copy.webp");
        var newPlayer = new Player("New") { Id = 2 };
        SetupPlayerLookup(1, oldPlayer);
        SetupPlayerLookup(2, newPlayer);

        await _service.RelinkAsync(1, LinkedUser(2));

        oldPlayer.Image.Should().BeNull();
        oldPlayer.ImageSource.Should().Be(PlayerImageSource.None);
        newPlayer.Image.Should().Be(CopiedImage);
        newPlayer.ImageSource.Should().Be(PlayerImageSource.Account);
        VerifyPlayerLoaded(1);
        VerifyPlayerLoaded(2);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Exactly(2));
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldDoNothing_WhenTheLinkDidNotChange()
    {
        await _service.RelinkAsync(2, LinkedUser(2));

        _playerRepositoryMock.Invocations.Should().BeEmpty();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyLinkedAccountImageAsync_ShouldCopyTheLinkedAccountPicture()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None))
            .ReturnsAsync(new PlayerLinkedAccountDto { Id = "u1", Username = "jane", ProfileImage = AccountImage });

        var result = await _service.CopyLinkedAccountImageAsync(4);

        result.Should().Be(CopiedImage);
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CopyLinkedAccountImageAsync_ShouldReturnNull_WhenNoAccountPictureExists()
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None))
            .ReturnsAsync((PlayerLinkedAccountDto?)null);

        var result = await _service.CopyLinkedAccountImageAsync(4);

        result.Should().BeNull();
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackfillAsync_ShouldGiveEveryWaitingPlayerTheAccountPicture()
    {
        var player = SetupPlayer(4);
        _userRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None))
            .ReturnsAsync([LinkedUser(4)]);

        var applied = await _service.BackfillAsync();

        applied.Should().Be(1);
        player.Image.Should().Be(CopiedImage);
        player.ImageSource.Should().Be(PlayerImageSource.Account);
        _userRepositoryMock.Verify(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None), Times.Once);
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackfillAsync_ShouldReturnZero_WhenNothingWaits()
    {
        _userRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None))
            .ReturnsAsync([]);

        var applied = await _service.BackfillAsync();

        applied.Should().Be(0);
        _userRepositoryMock.Verify(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldKeepThePreviousCopy_WhenTheNewFileCannotBeCopied()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");
        _imageServiceMock.Setup(x => x.CopyProfileImage(AccountImage)).ReturnsAsync((string?)null);

        var applied = await _service.ApplyAccountImageAsync(LinkedUser(4));

        applied.Should().BeFalse();
        player.Image.Should().Be("/images/profile/old_copy.webp");
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ApplyAccountImageAsync_ShouldKeepThePreviousFile_WhenSavingFails()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _service.ApplyAccountImageAsync(LinkedUser(4));

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        VerifyPlayerLoaded(4);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(CopiedImage), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldKeepTheFile_WhenSavingFails()
    {
        var player = SetupPlayer(4);
        player.UseAccountImage("/images/profile/old_copy.webp");
        _unitOfWorkMock
            .Setup(x => x.SaveChangesAsync(CancellationToken.None))
            .ThrowsAsync(new InvalidOperationException("db down"));

        var act = () => _service.ReleaseAsync(4);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("db down");
        VerifyPlayerLoaded(4);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDoNothing_WhenThePlayerIsGone()
    {
        SetupPlayerLookup(4, null);

        await _service.ReleaseAsync(4);

        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Never);
        VerifyPlayerLoaded(4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReleaseAsync_ShouldDoNothing_WhenThePlayerHasNoImage()
    {
        var player = SetupPlayer(4);

        await _service.ReleaseAsync(4);

        player.ImageSource.Should().Be(PlayerImageSource.None);
        VerifyPlayerLoaded(4);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldOnlyApplyTheNewPlayer_WhenThereWasNoPreviousPlayer()
    {
        var player = SetupPlayer(2);

        await _service.RelinkAsync(null, LinkedUser(2));

        player.Image.Should().Be(CopiedImage);
        VerifyPlayerLoaded(2);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task RelinkAsync_ShouldOnlyReleaseTheOldPlayer_WhenTheAccountIsUnlinked()
    {
        var player = SetupPlayer(1);
        player.UseAccountImage("/images/profile/old_copy.webp");

        await _service.RelinkAsync(1, new ApplicationUser("jane", null));

        player.Image.Should().BeNull();
        VerifyPlayerLoaded(1);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage("/images/profile/old_copy.webp"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FindLinkedAccountAsync_ShouldReturnTheAccountLinkedToThePlayer()
    {
        var account = new PlayerLinkedAccountDto { Id = "u1", Username = "jane" };
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None))
            .ReturnsAsync(account);

        var result = await _service.FindLinkedAccountAsync(4);

        result.Should().BeSameAs(account);
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public async Task CopyLinkedAccountImageAsync_ShouldReturnNull_WhenTheAccountHasNoPicture(string? profileImage)
    {
        _userRepositoryMock
            .Setup(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None))
            .ReturnsAsync(new PlayerLinkedAccountDto { Id = "u1", Username = "jane", ProfileImage = profileImage });

        var result = await _service.CopyLinkedAccountImageAsync(4);

        result.Should().BeNull();
        _userRepositoryMock.Verify(x => x.FirstOrDefaultAsync(It.Is<LinkedAccountByPlayerIdSpec>(s => IsLinkedTo(s, 4)), CancellationToken.None), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task BackfillAsync_ShouldOnlyCountThePlayersThatGotThePicture()
    {
        var applied = SetupPlayer(4);
        var uploaded = SetupPlayer(5, "/images/profile/own.webp");
        SetupPlayerLookup(6, null);
        _userRepositoryMock
            .Setup(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None))
            .ReturnsAsync([LinkedUser(4), LinkedUser(5), LinkedUser(6)]);

        var count = await _service.BackfillAsync();

        count.Should().Be(1);
        applied.Image.Should().Be(CopiedImage);
        uploaded.Image.Should().Be("/images/profile/own.webp");
        _userRepositoryMock.Verify(x => x.ListAsync(It.IsAny<AccountsAwaitingPlayerImageSpec>(), CancellationToken.None), Times.Once);
        VerifyPlayerLoaded(4);
        VerifyPlayerLoaded(5);
        VerifyPlayerLoaded(6);
        _imageServiceMock.Verify(x => x.CopyProfileImage(AccountImage), Times.Once);
        _unitOfWorkMock.Verify(x => x.SaveChangesAsync(CancellationToken.None), Times.Once);
        _imageServiceMock.Verify(x => x.DeleteImage(null), Times.Once);
        VerifyNoOtherCalls();
    }
}
