using System.Threading;
using System.Threading.Tasks;
using System.Collections.Generic;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Maintenance;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Maintenance;

public class ResetServiceTests
{
    private readonly Mock<IMaintenanceRepository> _maintenanceRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IManualService> _manualServiceMock;
    private readonly Mock<IMaintenanceSeeder> _maintenanceSeederMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
    private readonly Mock<IChangeDetectionClient> _changeDetectionClientMock = new();
    private readonly Mock<ISettingsService> _settingsServiceMock = new();
    private readonly Mock<ILogger<ResetService>> _loggerMock;
    private readonly ResetService _resetService;

    public ResetServiceTests()
    {
        _maintenanceRepositoryMock = new Mock<IMaintenanceRepository>();
        _unitOfWorkMock = new Mock<IUnitOfWork>();
        _imageServiceMock = new Mock<IImageService>();
        _manualServiceMock = new Mock<IManualService>();
        _maintenanceSeederMock = new Mock<IMaintenanceSeeder>();
        _transactionMock = new Mock<IDbContextTransaction>();
        _loggerMock = new Mock<ILogger<ResetService>>();

        _transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactionMock.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transactionMock.Object);
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync([]);

        _resetService = new ResetService(
            _maintenanceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _imageServiceMock.Object,
            _manualServiceMock.Object,
            _maintenanceSeederMock.Object,
            _changeDetectionClientMock.Object,
            _settingsServiceMock.Object,
            _loggerMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _maintenanceRepositoryMock.VerifyNoOtherCalls();
        _unitOfWorkMock.VerifyNoOtherCalls();
        _imageServiceMock.VerifyNoOtherCalls();
        _manualServiceMock.VerifyNoOtherCalls();
        _maintenanceSeederMock.VerifyNoOtherCalls();
        _transactionMock.VerifyNoOtherCalls();
        _changeDetectionClientMock.VerifyNoOtherCalls();
        _settingsServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldClearUserDataCommitAndClearImages()
    {
        await _resetService.ResetDataAsync();

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldNotCommitOrClearImages_WhenClearThrows()
    {
        _maintenanceRepositoryMock
            .Setup(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await _resetService.ResetDataAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Never);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldClearEverythingCommitClearImagesAndReseed()
    {
        await _resetService.FactoryResetAsync();

        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldNotCommitClearImagesOrReseed_WhenClearThrows()
    {
        _maintenanceRepositoryMock
            .Setup(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await _resetService.FactoryResetAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();

        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Never);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Never);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Never);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(It.IsAny<CancellationToken>()), Times.Never);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldNotTouchAnyData_WhenTheAdminPasswordIsRejected()
    {
        _maintenanceSeederMock
            .Setup(x => x.EnsureAdminPasswordAcceptedAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new ValidationException(Constants.Errors.AdminPasswordRejected));

        var act = async () => await _resetService.FactoryResetAsync();

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.AdminPasswordRejected);
        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldRemoveEveryPriceWatchAfterCommit_WithoutFailingOnErrors()
    {
        var calls = new List<string>();
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["w1", "w2", "w3"]);
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync(("http://cd", "key"));
        _transactionMock.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Callback(() => calls.Add("commit")).Returns(Task.CompletedTask);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync(It.IsAny<string>(), "http://cd", "key", It.IsAny<CancellationToken>()))
            .Callback((string id, string? _, string? _, CancellationToken _) => calls.Add(id))
            .ReturnsAsync(ChangeDetectionStatus.Ok);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w2", "http://cd", "key", It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("w2"))
            .ThrowsAsync(new InvalidOperationException("down"));
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w3", "http://cd", "key", It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("w3"))
            .ReturnsAsync(ChangeDetectionStatus.Unreachable);

        await _resetService.ResetDataAsync();

        calls.Should().Equal("commit", "w1", "w2", "w3");
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.GetChangeDetectionSettingsAsync(), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync(It.IsAny<string>(), "http://cd", "key", It.IsAny<CancellationToken>()), Times.Exactly(3));
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldRemovePriceWatchesWithTheSettingsReadBeforeTheyAreWiped()
    {
        var calls = new List<string>();
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(["w1"]);
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .Callback(() => calls.Add("settings"))
            .ReturnsAsync(("http://cd", "key"));
        _maintenanceRepositoryMock
            .Setup(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("wipe"))
            .Returns(Task.CompletedTask);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w1", "http://cd", "key", It.IsAny<CancellationToken>()))
            .Callback(() => calls.Add("w1"))
            .ReturnsAsync(ChangeDetectionStatus.Ok);

        await _resetService.FactoryResetAsync();

        calls.Should().Equal("settings", "wipe", "w1");
        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(It.IsAny<CancellationToken>()), Times.Once);
        _settingsServiceMock.Verify(x => x.GetChangeDetectionSettingsAsync(), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync("w1", "http://cd", "key", It.IsAny<CancellationToken>()), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }
}
