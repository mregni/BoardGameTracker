using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Integrations.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Integrations;

public class IntegrationsControllerTests
{
    private readonly Mock<IIntegrationStatusService> _serviceMock = new();
    private readonly IntegrationsController _controller;

    public IntegrationsControllerTests()
    {
        _controller = new IntegrationsController(_serviceMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _serviceMock.VerifyNoOtherCalls();
    }

    private static IntegrationStatusDto Status(IntegrationKind integration, IntegrationConnectionStatus status) => new()
    {
        Integration = integration,
        Status = status,
        CheckedAt = new DateTime(2026, 10, 16, 9, 30, 0, DateTimeKind.Utc),
        MessageKey = "ok"
    };

    [Fact]
    public async Task GetStatus_ShouldReturnEveryIntegration()
    {
        var statuses = new List<IntegrationStatusDto>
        {
            Status(IntegrationKind.Bgg, IntegrationConnectionStatus.Ok),
            Status(IntegrationKind.ChangeDetection, IntegrationConnectionStatus.NotConfigured)
        };
        _serviceMock.Setup(x => x.GetAllAsync(It.IsAny<CancellationToken>())).ReturnsAsync(statuses);

        var result = await _controller.GetStatus(CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(statuses);
        _serviceMock.Verify(x => x.GetAllAsync(It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("bgg", IntegrationKind.Bgg)]
    [InlineData("BGG", IntegrationKind.Bgg)]
    [InlineData("changedetection", IntegrationKind.ChangeDetection)]
    [InlineData("changeDetection", IntegrationKind.ChangeDetection)]
    public async Task Test_ShouldTestTheRequestedIntegration(string integration, IntegrationKind expected)
    {
        var status = Status(expected, IntegrationConnectionStatus.Unauthorized);
        _serviceMock.Setup(x => x.TestAsync(expected, It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var result = await _controller.Test(integration, CancellationToken.None);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(status);
        _serviceMock.Verify(x => x.TestAsync(expected, It.IsAny<CancellationToken>()), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("1")]
    [InlineData("0")]
    [InlineData("openai")]
    public async Task Test_ShouldReturnNotFound_ForAnUnknownIntegration(string integration)
    {
        var result = await _controller.Test(integration, CancellationToken.None);

        result.Should().BeOfType<NotFoundResult>();
        VerifyNoOtherCalls();
    }
}
