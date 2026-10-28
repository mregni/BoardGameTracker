using System.Threading;
using System.Threading.Tasks;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.ChangeDetection;
using BoardGameTracker.Common.Models.Integrations;
using BoardGameTracker.Core.ChangeDetection.Interfaces;
using BoardGameTracker.Core.Integrations;
using FluentAssertions;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Integrations;

public class ChangeDetectionConnectionTesterTests
{
    private readonly Mock<IChangeDetectionClient> _clientMock = new();
    private readonly ChangeDetectionConnectionTester _tester;

    public ChangeDetectionConnectionTesterTests()
    {
        _tester = new ChangeDetectionConnectionTester(_clientMock.Object);
    }

    [Fact]
    public void Integration_ShouldBeChangeDetection()
    {
        _tester.Integration.Should().Be(IntegrationKind.ChangeDetection);
        _clientMock.VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(ChangeDetectionStatus.Ok, IntegrationConnectionStatus.Ok, IntegrationMessageKeys.Ok)]
    [InlineData(ChangeDetectionStatus.NotConfigured, IntegrationConnectionStatus.NotConfigured, IntegrationMessageKeys.NotConfigured)]
    [InlineData(ChangeDetectionStatus.Unauthorized, IntegrationConnectionStatus.Unauthorized, IntegrationMessageKeys.Unauthorized)]
    [InlineData(ChangeDetectionStatus.Misconfigured, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.Misconfigured)]
    [InlineData(ChangeDetectionStatus.Unreachable, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.Unreachable)]
    [InlineData(ChangeDetectionStatus.WatchNotFound, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.Unreachable)]
    [InlineData(ChangeDetectionStatus.ParseError, IntegrationConnectionStatus.Unreachable, IntegrationMessageKeys.Unreachable)]
    public async Task TestAsync_ShouldMapTheConnectionTest(ChangeDetectionStatus status, IntegrationConnectionStatus expected, string messageKey)
    {
        _clientMock.Setup(x => x.TestConnectionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(status);

        var result = await _tester.TestAsync();

        result.Should().Be(new IntegrationTestResult(expected, messageKey));
        _clientMock.Verify(x => x.TestConnectionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _clientMock.VerifyNoOtherCalls();
    }
}
