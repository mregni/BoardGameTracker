using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Maintenance;
using BoardGameTracker.Core.Maintenance.Interfaces;
using BoardGameTracker.Core.Manuals.Interfaces;
using FluentAssertions;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Services;

public class ResetServiceTests
{
    private readonly Mock<IMaintenanceRepository> _maintenanceRepositoryMock;
    private readonly Mock<IUnitOfWork> _unitOfWorkMock;
    private readonly Mock<IImageService> _imageServiceMock;
    private readonly Mock<IManualService> _manualServiceMock;
    private readonly Mock<IMaintenanceSeeder> _maintenanceSeederMock;
    private readonly Mock<IDbContextTransaction> _transactionMock;
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

        _resetService = new ResetService(
            _maintenanceRepositoryMock.Object,
            _unitOfWorkMock.Object,
            _imageServiceMock.Object,
            _manualServiceMock.Object,
            _maintenanceSeederMock.Object,
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
    }

    [Fact]
    public async Task ResetDataAsync_ShouldClearUserDataCommitAndClearImages()
    {
        await _resetService.ResetDataAsync();

        _unitOfWorkMock.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
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

    [Fact]
    public async Task FactoryResetAsync_ShouldNotCommitClearImagesOrReseed_WhenClearThrows()
    {
        _maintenanceRepositoryMock
            .Setup(x => x.ClearSettingsAndAuthAsync(It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("boom"));

        var act = async () => await _resetService.FactoryResetAsync();

        await act.Should().ThrowAsync<InvalidOperationException>();

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
}
