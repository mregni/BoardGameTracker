using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.ChangeDetection;

public class ChangeDetectionControllerTests
{
    private const string WatchId = "e0808154-28da-4b85-9a71-24a409e694f1";

    private readonly Mock<IChangeDetectionClient> _clientMock = new();
    private readonly ChangeDetectionController _controller;
    private readonly CancellationToken _token = new CancellationTokenSource().Token;

    public ChangeDetectionControllerTests()
    {
        _controller = new ChangeDetectionController(_clientMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _clientMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatch_ShouldReturnTheWatch_WhenItExists()
    {
        var info = new ChangeDetectionWatchInfo("https://shop.example.com/brass", "Brass", new DateTime(2026, 8, 28, 12, 0, 0, DateTimeKind.Utc));
        _clientMock.Setup(x => x.GetWatchInfoAsync(WatchId, _token)).ReturnsAsync((ChangeDetectionStatus.Ok, info));

        var result = await _controller.GetWatch(WatchId, _token);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(info);
        _clientMock.Verify(x => x.GetWatchInfoAsync(WatchId, _token), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetWatch_ShouldReturnBadRequest_WithoutCallingTheInstance_WhenTheIdIsNotAGuid()
    {
        var result = await _controller.GetWatch("../systeminfo", _token);

        result.Should().BeOfType<BadRequestResult>();
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ChangeDetectionStatus.WatchNotFound, StatusCodes.Status404NotFound)]
    [InlineData(ChangeDetectionStatus.NotConfigured, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(ChangeDetectionStatus.Misconfigured, StatusCodes.Status503ServiceUnavailable)]
    [InlineData(ChangeDetectionStatus.Unauthorized, StatusCodes.Status502BadGateway)]
    [InlineData(ChangeDetectionStatus.Unreachable, StatusCodes.Status502BadGateway)]
    [InlineData(ChangeDetectionStatus.ParseError, StatusCodes.Status502BadGateway)]
    [InlineData(ChangeDetectionStatus.Pending, StatusCodes.Status502BadGateway)]
    [InlineData(ChangeDetectionStatus.Ok, StatusCodes.Status502BadGateway)]
    public async Task GetWatch_ShouldMapTheInstanceStatus_ToAnHttpStatus(ChangeDetectionStatus status, int expected)
    {
        _clientMock.Setup(x => x.GetWatchInfoAsync(WatchId, _token)).ReturnsAsync((status, (ChangeDetectionWatchInfo?)null));

        var result = await _controller.GetWatch(WatchId, _token);

        result.Should().BeAssignableTo<IStatusCodeActionResult>().Which.StatusCode.Should().Be(expected);
        _clientMock.Verify(x => x.GetWatchInfoAsync(WatchId, _token), Times.Once);
        VerifyNoOtherCalls();
    }
}
