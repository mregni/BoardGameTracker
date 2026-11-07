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
using BoardGameTracker.Tests.Support;
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
    private readonly CancellationToken _token = new CancellationTokenSource().Token;
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

        _transactionMock.Setup(x => x.CommitAsync(_token)).Returns(Task.CompletedTask);
        _transactionMock.Setup(x => x.DisposeAsync()).Returns(ValueTask.CompletedTask);
        _unitOfWorkMock
            .Setup(x => x.BeginTransactionAsync(_token))
            .ReturnsAsync(_transactionMock.Object);
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(_token))
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
        _loggerMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldClearUserDataCommitAndClearImages()
    {
        await _resetService.ResetDataAsync(_token);

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting data reset");
        _loggerMock.VerifyLog(LogLevel.Information, "Data reset completed");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldNotCommitOrClearImages_WhenClearThrows()
    {
        _maintenanceRepositoryMock
            .Setup(x => x.ClearUserDataAsync(_token))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await _resetService.ResetDataAsync(_token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Never);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Never);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Never);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting data reset");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldClearEverythingCommitClearImagesAndReseed()
    {
        await _resetService.FactoryResetAsync(_token);

        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(_token), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting factory reset");
        _loggerMock.VerifyLog(LogLevel.Information, "Factory reset completed");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldNotCommitClearImagesOrReseed_WhenClearThrows()
    {
        _maintenanceRepositoryMock
            .Setup(x => x.ClearSettingsAndAuthAsync(_token))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await _resetService.FactoryResetAsync(_token);

        await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("boom");

        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Never);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Never);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Never);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(_token), Times.Never);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting factory reset");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldNotTouchAnyData_WhenTheAdminPasswordIsRejected()
    {
        _maintenanceSeederMock
            .Setup(x => x.EnsureAdminPasswordAcceptedAsync(_token))
            .ThrowsAsync(new ValidationException(Constants.Errors.AdminPasswordRejected));

        var act = async () => await _resetService.FactoryResetAsync(_token);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.AdminPasswordRejected);
        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting factory reset");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ResetDataAsync_ShouldRemoveEveryPriceWatchAfterCommit_WithoutFailingOnErrors()
    {
        var calls = new List<string>();
        var failure = new InvalidOperationException("down");
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(_token))
            .ReturnsAsync(["w1", "w2", "w3"]);
        _settingsServiceMock.Setup(x => x.GetChangeDetectionSettingsAsync()).ReturnsAsync(("http://cd", "key"));
        _transactionMock.Setup(x => x.CommitAsync(_token)).Callback(() => calls.Add("commit")).Returns(Task.CompletedTask);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w1", "http://cd", "key", CancellationToken.None))
            .Callback(() => calls.Add("w1"))
            .ReturnsAsync(ChangeDetectionStatus.Ok);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w2", "http://cd", "key", CancellationToken.None))
            .Callback(() => calls.Add("w2"))
            .ThrowsAsync(failure);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w3", "http://cd", "key", CancellationToken.None))
            .Callback(() => calls.Add("w3"))
            .ReturnsAsync(ChangeDetectionStatus.Unreachable);

        await _resetService.ResetDataAsync(_token);

        calls.Should().Equal("commit", "w1", "w2", "w3");
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _settingsServiceMock.Verify(x => x.GetChangeDetectionSettingsAsync(), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync("w1", "http://cd", "key", CancellationToken.None), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync("w2", "http://cd", "key", CancellationToken.None), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync("w3", "http://cd", "key", CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting data reset");
        _loggerMock.VerifyLog(LogLevel.Warning, "Could not remove price watch w2", failure);
        _loggerMock.VerifyLog(LogLevel.Warning, "Could not remove price watch w3: Unreachable");
        _loggerMock.VerifyLog(LogLevel.Information, "Data reset completed");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryResetAsync_ShouldRemovePriceWatchesWithTheSettingsReadBeforeTheyAreWiped()
    {
        var calls = new List<string>();
        _maintenanceRepositoryMock
            .Setup(x => x.GetChangeDetectionWatchIdsAsync(_token))
            .ReturnsAsync(["w1"]);
        _settingsServiceMock
            .Setup(x => x.GetChangeDetectionSettingsAsync())
            .Callback(() => calls.Add("settings"))
            .ReturnsAsync(("http://cd", "key"));
        _maintenanceRepositoryMock
            .Setup(x => x.ClearSettingsAndAuthAsync(_token))
            .Callback(() => calls.Add("wipe"))
            .Returns(Task.CompletedTask);
        _changeDetectionClientMock
            .Setup(x => x.DeleteWatchAsync("w1", "http://cd", "key", CancellationToken.None))
            .Callback(() => calls.Add("w1"))
            .ReturnsAsync(ChangeDetectionStatus.Ok);

        await _resetService.FactoryResetAsync(_token);

        calls.Should().Equal("settings", "wipe", "w1");
        _maintenanceSeederMock.Verify(x => x.EnsureAdminPasswordAcceptedAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.GetChangeDetectionWatchIdsAsync(_token), Times.Once);
        _settingsServiceMock.Verify(x => x.GetChangeDetectionSettingsAsync(), Times.Once);
        _changeDetectionClientMock.Verify(x => x.DeleteWatchAsync("w1", "http://cd", "key", CancellationToken.None), Times.Once);
        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearUserDataAsync(_token), Times.Once);
        _maintenanceRepositoryMock.Verify(x => x.ClearSettingsAndAuthAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.CommitAsync(_token), Times.Once);
        _transactionMock.Verify(x => x.DisposeAsync(), Times.Once);
        _imageServiceMock.Verify(x => x.ClearAllImages(), Times.Once);
        _manualServiceMock.Verify(x => x.ClearAllManuals(), Times.Once);
        _maintenanceSeederMock.Verify(x => x.ReseedDefaultsAsync(_token), Times.Once);
        _loggerMock.VerifyLog(LogLevel.Information, "Starting factory reset");
        _loggerMock.VerifyLog(LogLevel.Information, "Factory reset completed");
        VerifyNoOtherCalls();
    }
}
