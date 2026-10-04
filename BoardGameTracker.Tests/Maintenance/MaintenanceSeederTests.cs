using System;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Maintenance;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Maintenance;

public class MaintenanceSeederTests : IDisposable
{
    private readonly IdentityTestScope _identity = new();
    private readonly Mock<IConfigRepository> _configRepositoryMock = new();
    private readonly Mock<IEnvironmentProvider> _environmentProviderMock = new();
    private readonly MaintenanceSeeder _seeder;

    public MaintenanceSeederTests()
    {
        _seeder = new MaintenanceSeeder(
            _configRepositoryMock.Object,
            _environmentProviderMock.Object,
            _identity.RoleManager,
            _identity.UserManager,
            Mock.Of<ILogger<MaintenanceSeeder>>());
    }

    public void Dispose()
    {
        _identity.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _configRepositoryMock.VerifyNoOtherCalls();
        _environmentProviderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureAdminPasswordAcceptedAsync_ShouldNotCheckAnything_WhenAuthenticationIsDisabled()
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(false);

        await _seeder.EnsureAdminPasswordAcceptedAsync();

        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task EnsureAdminPasswordAcceptedAsync_ShouldRefuse_WhenAdminPasswordBreaksTheRules()
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.AdminPassword).Returns("short");

        var act = () => _seeder.EnsureAdminPasswordAcceptedAsync();

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.AdminPasswordRejected);
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AdminPassword, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("a-long-enough-password")]
    public async Task EnsureAdminPasswordAcceptedAsync_ShouldAccept_AnUnsetOrValidAdminPassword(string? password)
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.AdminPassword).Returns(password);

        await _seeder.EnsureAdminPasswordAcceptedAsync();

        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AdminPassword, Times.Once);
        VerifyNoOtherCalls();
    }
}
