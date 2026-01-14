using System;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.Updates;
using BoardGameTracker.Core.Updates.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class UpdateControllerTests
{
    private readonly Mock<IUpdateService> _updateServiceMock;
    private readonly UpdateController _controller;

    public UpdateControllerTests()
    {
        _updateServiceMock = new Mock<IUpdateService>();
        _controller = new UpdateController(_updateServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _updateServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenUpdateIsAvailable()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "1.1.0",
            UpdateAvailable = true,
            LastChecked = DateTime.UtcNow.AddHours(-1),
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetUpdateStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.0.0");
        statusDto.LatestVersion.Should().Be("1.1.0");
        statusDto.UpdateAvailable.Should().BeTrue();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenNoUpdateIsAvailable()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "2.5.0",
            LatestVersion = "2.5.0",
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow.AddMinutes(-30),
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetUpdateStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("2.5.0");
        statusDto.LatestVersion.Should().Be("2.5.0");
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenNeverChecked()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.0.0",
            LatestVersion = null,
            UpdateAvailable = false,
            LastChecked = null,
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetUpdateStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.0.0");
        statusDto.LatestVersion.Should().BeNull();
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().BeNull();
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetUpdateStatus_ShouldReturnStatus_WhenErrorOccurred()
    {
        // Arrange
        var updateStatus = new UpdateStatus
        {
            CurrentVersion = "1.5.0",
            LatestVersion = null,
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow.AddDays(-1),
            ErrorMessage = "Failed to connect to update server"
        };

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updateStatus);

        // Act
        var result = await _controller.GetUpdateStatus();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.5.0");
        statusDto.LatestVersion.Should().BeNull();
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().Be("Failed to connect to update server");

        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckNow_ShouldCheckForUpdatesAndReturnStatus_WhenUpdateIsAvailable()
    {
        // Arrange
        var updatedStatus = new UpdateStatus
        {
            CurrentVersion = "1.0.0",
            LatestVersion = "1.2.0",
            UpdateAvailable = true,
            LastChecked = DateTime.UtcNow,
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updatedStatus);

        // Act
        var result = await _controller.CheckNow();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("1.0.0");
        statusDto.LatestVersion.Should().Be("1.2.0");
        statusDto.UpdateAvailable.Should().BeTrue();
        statusDto.LastChecked.Should().Be(updatedStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckNow_ShouldCheckForUpdatesAndReturnStatus_WhenNoUpdateIsAvailable()
    {
        // Arrange
        var updatedStatus = new UpdateStatus
        {
            CurrentVersion = "3.0.0",
            LatestVersion = "3.0.0",
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow,
            ErrorMessage = null
        };

        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(updatedStatus);

        // Act
        var result = await _controller.CheckNow();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("3.0.0");
        statusDto.LatestVersion.Should().Be("3.0.0");
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(updatedStatus.LastChecked);
        statusDto.ErrorMessage.Should().BeNull();

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckNow_ShouldCheckForUpdatesAndReturnStatus_WhenErrorOccurs()
    {
        // Arrange
        var errorStatus = new UpdateStatus
        {
            CurrentVersion = "2.0.0",
            LatestVersion = null,
            UpdateAvailable = false,
            LastChecked = DateTime.UtcNow,
            ErrorMessage = "Network timeout"
        };

        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ReturnsAsync(errorStatus);

        // Act
        var result = await _controller.CheckNow();

        // Assert
        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be("2.0.0");
        statusDto.LatestVersion.Should().BeNull();
        statusDto.UpdateAvailable.Should().BeFalse();
        statusDto.LastChecked.Should().Be(errorStatus.LastChecked);
        statusDto.ErrorMessage.Should().Be("Network timeout");

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckNow_ShouldThrowException_WhenCheckForUpdatesThrows()
    {
        // Arrange
        var expectedException = new InvalidOperationException("Update check failed");

        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<InvalidOperationException>(
            () => _controller.CheckNow());

        exception.Should().Be(expectedException);

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CheckNow_ShouldThrowException_WhenGetUpdateStatusThrows()
    {
        // Arrange
        var expectedException = new TimeoutException("Status retrieval timeout");

        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.GetUpdateStatusAsync())
            .ThrowsAsync(expectedException);

        // Act & Assert
        var exception = await Assert.ThrowsAsync<TimeoutException>(
            () => _controller.CheckNow());

        exception.Should().Be(expectedException);

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _updateServiceMock.Verify(x => x.GetUpdateStatusAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
