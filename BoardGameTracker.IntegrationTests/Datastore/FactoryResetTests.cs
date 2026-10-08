using System.Net;
using System.Net.Http.Json;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Datastore;

[Collection(IntegrationCollection.Name)]
public class FactoryResetTests
{
    private readonly IntegrationFixture _fixture;

    public FactoryResetTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task FactoryReset_ShouldLeaveOnlyAFreshAdmin_WithDefaultSettings_AndNoSingleSignOn()
    {
        using var admin = await _fixture.CreateAdminClientAsync();
        await using (var scope = _fixture.CreateScope())
        {
            var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
            var user = await context.Users.SingleAsync(u => u.UserName == "user", TestContext.Current.CancellationToken);
            context.OidcProviders.Add(new OidcProvider("factory-idp", "Factory IdP", "https://idp.example.com", "bgt-client"));
            context.ExternalLogins.Add(new ExternalLogin(user.Id, "factory-idp", "factory-subject"));
            await context.SaveChangesAsync(TestContext.Current.CancellationToken);
            await context.Config
                .Where(c => c.Key == Constants.AppConfig.Currency)
                .ExecuteUpdateAsync(s => s.SetProperty(c => c.Value, "CHF"), TestContext.Current.CancellationToken);
        }

        try
        {
            var reset = await admin.PostAsync("/api/maintenance/factory-reset", null, TestContext.Current.CancellationToken);
            reset.StatusCode.Should().Be(HttpStatusCode.NoContent, await reset.Content.ReadAsStringAsync(TestContext.Current.CancellationToken));

            await using (var scope = _fixture.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();
                (await context.Users.Select(u => u.UserName).ToListAsync(TestContext.Current.CancellationToken))
                    .Should().Equal(IntegrationFixture.AdminUsername);
                (await context.Roles.Select(r => r.Name).ToListAsync(TestContext.Current.CancellationToken))
                    .Should().BeEquivalentTo(Constants.AuthRoles.Admin, Constants.AuthRoles.User, Constants.AuthRoles.Reader);
                (await context.OidcProviders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
                (await context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
                (await context.RefreshTokens.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
            }

            using var anonymous = _fixture.CreateClient();
            var login = await anonymous.PostAsJsonAsync("/api/auth/login", new LoginRequest(IntegrationFixture.AdminUsername, IntegrationFixture.AdminPassword), TestContext.Current.CancellationToken);
            login.StatusCode.Should().Be(HttpStatusCode.OK);
            var session = (await login.Content.ReadFromJsonAsync<LoginResponse>(IntegrationFixture.Json, TestContext.Current.CancellationToken))!;
            session.User.Roles.Should().Contain(Constants.AuthRoles.Admin);

            var settings = await anonymous.GetFromJsonAsync<UIResourceDto>("/api/settings", IntegrationFixture.Json, TestContext.Current.CancellationToken);
            settings!.Currency.Should().Be("€");
        }
        finally
        {
            await _fixture.RestoreTestUsersAsync();
        }
    }
}
