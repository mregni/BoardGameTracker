using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Maintenance;
using BoardGameTracker.Tests.Support;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
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
            NullLogger<MaintenanceSeeder>.Instance);
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

        var act = () => _seeder.EnsureAdminPasswordAcceptedAsync();

        await act.Should().NotThrowAsync();
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("short")]
    [InlineData("1234567")]
    public async Task EnsureAdminPasswordAcceptedAsync_ShouldRefuse_WhenAdminPasswordBreaksTheRules(string password)
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.AdminPassword).Returns(password);

        var act = () => _seeder.EnsureAdminPasswordAcceptedAsync();

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.AdminPasswordRejected);
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AdminPassword, Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("   ")]
    [InlineData("12345678")]
    [InlineData("a-long-enough-password")]
    public async Task EnsureAdminPasswordAcceptedAsync_ShouldAccept_AnUnsetOrValidAdminPassword(string? password)
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.AdminPassword).Returns(password);

        var act = () => _seeder.EnsureAdminPasswordAcceptedAsync();

        await act.Should().NotThrowAsync();
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AdminPassword, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReseedDefaultsAsync_ShouldSeedTheConfigOnly_WhenAuthenticationIsDisabled()
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(false);

        await _seeder.ReseedDefaultsAsync();

        (await _identity.Context.Roles.CountAsync()).Should().Be(0);
        (await _identity.Context.Users.CountAsync()).Should().Be(0);
        _configRepositoryMock.Verify(x => x.SeedConfigAsync(ConfigDefaults.All), Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ReseedDefaultsAsync_ShouldRecreateRolesAndTheAdminWithTheConfiguredPassword_WhenAuthenticationIsEnabled()
    {
        _environmentProviderMock.SetupGet(x => x.AuthEnabled).Returns(true);
        _environmentProviderMock.SetupGet(x => x.AdminPassword).Returns("a-long-enough-password");

        await _seeder.ReseedDefaultsAsync();

        var roles = await _identity.Context.Roles.Select(x => x.Name).ToListAsync();
        roles.Should().BeEquivalentTo(Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader);
        var admin = await _identity.UserManager.FindByNameAsync("admin");
        admin.Should().NotBeNull();
        (await _identity.UserManager.CheckPasswordAsync(admin!, "a-long-enough-password")).Should().BeTrue();
        (await _identity.UserManager.IsInRoleAsync(admin!, Constants.AuthRoles.Admin)).Should().BeTrue();
        _configRepositoryMock.Verify(x => x.SeedConfigAsync(ConfigDefaults.All), Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AuthEnabled, Times.Once);
        _environmentProviderMock.VerifyGet(x => x.AdminPassword, Times.Once);
        VerifyNoOtherCalls();
    }
}
