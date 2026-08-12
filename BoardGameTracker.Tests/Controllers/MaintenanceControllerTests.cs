using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Core.Maintenance.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Controllers;

public class MaintenanceControllerTests
{
    private readonly Mock<IResetService> _resetServiceMock;
    private readonly MaintenanceController _controller;

    public MaintenanceControllerTests()
    {
        _resetServiceMock = new Mock<IResetService>();
        _controller = new MaintenanceController(_resetServiceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _resetServiceMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Reset_ShouldReturnNoContent()
    {
        _resetServiceMock.Setup(x => x.ResetDataAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.Reset();

        result.Should().BeOfType<NoContentResult>();
        _resetServiceMock.Verify(x => x.ResetDataAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task FactoryReset_ShouldReturnNoContent()
    {
        _resetServiceMock.Setup(x => x.FactoryResetAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await _controller.FactoryReset();

        result.Should().BeOfType<NoContentResult>();
        _resetServiceMock.Verify(x => x.FactoryResetAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

}
