using System;
using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Integrations;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Integrations;
using BoardGameTracker.Core.Integrations.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Caching.Memory;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Integrations;

public class IntegrationStatusServiceTests
{
    private static readonly DateTime Now = new(2026, 10, 16, 9, 30, 0, DateTimeKind.Utc);

    private readonly Mock<IIntegrationTester> _bggTesterMock;
    private readonly Mock<IIntegrationTester> _changeDetectionTesterMock;
    private readonly Mock<IDateTimeProvider> _dateTimeProviderMock;
    private readonly IntegrationStatusService _service;

    public IntegrationStatusServiceTests()
    {
        _bggTesterMock = new Mock<IIntegrationTester>();
        _bggTesterMock.SetupGet(x => x.Integration).Returns(IntegrationKind.Bgg);
        _bggTesterMock.Setup(x => x.TestAsync(It.IsAny<CancellationToken>())).ReturnsAsync(IntegrationTestResult.Ok);

        _changeDetectionTesterMock = new Mock<IIntegrationTester>();
        _changeDetectionTesterMock.SetupGet(x => x.Integration).Returns(IntegrationKind.ChangeDetection);
        _changeDetectionTesterMock.Setup(x => x.TestAsync(It.IsAny<CancellationToken>())).ReturnsAsync(IntegrationTestResult.NotConfigured);

        _dateTimeProviderMock = new Mock<IDateTimeProvider>();
        _dateTimeProviderMock.SetupGet(x => x.UtcNow).Returns(Now);

        _service = new IntegrationStatusService(
            [_changeDetectionTesterMock.Object, _bggTesterMock.Object],
            new MemoryCache(new MemoryCacheOptions()),
            _dateTimeProviderMock.Object);
    }

    private void VerifyNoOtherCalls()
    {
        _bggTesterMock.VerifyNoOtherCalls();
        _changeDetectionTesterMock.VerifyNoOtherCalls();
        _dateTimeProviderMock.VerifyNoOtherCalls();
    }

    private void VerifyTesterSetup()
    {
        _bggTesterMock.VerifyGet(x => x.Integration, Times.AtLeastOnce);
        _changeDetectionTesterMock.VerifyGet(x => x.Integration, Times.AtLeastOnce);
    }

    [Fact]
    public async Task GetAllAsync_ShouldTestEveryIntegration_InAStableOrder()
    {
        var statuses = await _service.GetAllAsync();

        statuses.Should().HaveCount(2);
        statuses[0].Integration.Should().Be(IntegrationKind.Bgg);
        statuses[0].Status.Should().Be(IntegrationConnectionStatus.Ok);
        statuses[0].MessageKey.Should().Be(IntegrationMessageKeys.Ok);
        statuses[0].CheckedAt.Should().Be(Now);
        statuses[1].Integration.Should().Be(IntegrationKind.ChangeDetection);
        statuses[1].Status.Should().Be(IntegrationConnectionStatus.NotConfigured);
        _bggTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Once);
        _changeDetectionTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(2));
        VerifyTesterSetup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetAllAsync_ShouldServeTheCachedStatus_OnTheSecondCall()
    {
        var first = await _service.GetAllAsync();
        var second = await _service.GetAllAsync();

        second.Should().Equal(first);
        _bggTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Once);
        _changeDetectionTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(2));
        VerifyTesterSetup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestAsync_ShouldBypassTheCache_AndRefreshIt()
    {
        await _service.GetAllAsync();
        _bggTesterMock.Setup(x => x.TestAsync(It.IsAny<CancellationToken>())).ReturnsAsync(IntegrationTestResult.Unauthorized);

        var tested = await _service.TestAsync(IntegrationKind.Bgg);
        var all = await _service.GetAllAsync();

        tested.Integration.Should().Be(IntegrationKind.Bgg);
        tested.Status.Should().Be(IntegrationConnectionStatus.Unauthorized);
        tested.MessageKey.Should().Be(IntegrationMessageKeys.Unauthorized);
        all[0].Should().BeSameAs(tested);
        _bggTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _changeDetectionTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Once);
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(3));
        VerifyTesterSetup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task Invalidate_ShouldForceANewTest_OnTheNextCall()
    {
        await _service.GetAllAsync();

        _service.Invalidate();
        await _service.GetAllAsync();

        _bggTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _changeDetectionTesterMock.Verify(x => x.TestAsync(It.IsAny<CancellationToken>()), Times.Exactly(2));
        _dateTimeProviderMock.VerifyGet(x => x.UtcNow, Times.Exactly(4));
        VerifyTesterSetup();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void CacheDuration_ShouldBeShort()
    {
        IntegrationStatusService.CacheDuration.Should().Be(TimeSpan.FromMinutes(5));
    }
}
