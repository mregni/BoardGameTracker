using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Auth;

[Collection(IntegrationCollection.Name)]
public class AdminSeedingTests
{
    private readonly IntegrationFixture _fixture;

    public AdminSeedingTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task SeedAuthData_ShouldNotRecreateAdmin_WhenTheAdminWasRenamedAndOtherUsersExist()
    {
        await using var scope = _fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var admin = await userManager.FindByNameAsync(IntegrationFixture.AdminUsername);
        var usersBefore = await userManager.Users.CountAsync(TestContext.Current.CancellationToken);

        (await userManager.SetUserNameAsync(admin!, "renamed-admin")).Succeeded.Should().BeTrue();
        try
        {
            await DbSeeder.SeedAuthData(roleManager, userManager, NullLogger.Instance, IntegrationFixture.AdminPassword);

            (await userManager.FindByNameAsync(IntegrationFixture.AdminUsername)).Should().BeNull();
            (await userManager.Users.CountAsync(TestContext.Current.CancellationToken)).Should().Be(usersBefore);
        }
        finally
        {
            (await userManager.SetUserNameAsync(admin!, IntegrationFixture.AdminUsername)).Succeeded.Should().BeTrue();
        }
    }

    [Fact]
    public async Task GetAdminPasswordErrors_ShouldRejectAShortPassword_AndAcceptAValidOne()
    {
        await using var scope = _fixture.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();

        var shortPassword = await DbSeeder.GetAdminPasswordErrorsAsync(userManager, "admin1");
        var validPassword = await DbSeeder.GetAdminPasswordErrorsAsync(userManager, IntegrationFixture.AdminPassword);
        var unset = await DbSeeder.GetAdminPasswordErrorsAsync(userManager, null);

        shortPassword.Should().NotBeEmpty();
        validPassword.Should().BeEmpty();
        unset.Should().BeEmpty();
    }
}
