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

    public static TheoryData<UpdateStatus> UpdateStatuses => new()
    {
        new UpdateStatus { CurrentVersion = "1.0.0", LatestVersion = "1.2.0", UpdateAvailable = true, LastChecked = DateTime.UtcNow, ErrorMessage = null },
        new UpdateStatus { CurrentVersion = "3.0.0", LatestVersion = "3.0.0", UpdateAvailable = false, LastChecked = DateTime.UtcNow, ErrorMessage = null },
        new UpdateStatus { CurrentVersion = "2.0.0", LatestVersion = null, UpdateAvailable = false, LastChecked = DateTime.UtcNow, ErrorMessage = "Network timeout" }
    };

    [Theory]
    [MemberData(nameof(UpdateStatuses))]
    public async Task CheckNow_ShouldCheckForUpdatesAndReturnStatus(UpdateStatus updateStatus)
    {
        _updateServiceMock
            .Setup(x => x.CheckForUpdatesAsync())
            .Returns(Task.CompletedTask);

        _updateServiceMock
            .Setup(x => x.GetVersionInfoAsync())
            .ReturnsAsync(updateStatus);

        var result = await _controller.CheckNow();

        var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
        var statusDto = okResult.Value.Should().BeAssignableTo<UpdateStatusDto>().Subject;

        statusDto.CurrentVersion.Should().Be(updateStatus.CurrentVersion);
        statusDto.LatestVersion.Should().Be(updateStatus.LatestVersion);
        statusDto.UpdateAvailable.Should().Be(updateStatus.UpdateAvailable);
        statusDto.LastChecked.Should().Be(updateStatus.LastChecked);
        statusDto.ErrorMessage.Should().Be(updateStatus.ErrorMessage);

        _updateServiceMock.Verify(x => x.CheckForUpdatesAsync(), Times.Once);
        _updateServiceMock.Verify(x => x.GetVersionInfoAsync(), Times.Once);
        VerifyNoOtherCalls();
    }
}
