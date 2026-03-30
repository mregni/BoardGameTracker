using System;
using System.Threading.Tasks;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcProviderServiceTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly IMemoryCache _cache;
    private readonly Mock<ILogger<OidcProviderService>> _loggerMock;
    private readonly OidcProviderService _service;

    public OidcProviderServiceTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        _cache = new MemoryCache(new MemoryCacheOptions());
        _loggerMock = new Mock<ILogger<OidcProviderService>>();
        _service = new OidcProviderService(_context, _cache, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
        _cache.Dispose();
        GC.SuppressFinalize(this);
    }

    #region GetAllAsync

    [Fact]
    public async Task GetAllAsync_ShouldReturnEmptyList_WhenNoProvidersExist()
    {
        var result = await _service.GetAllAsync();

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task GetAllAsync_ShouldReturnAllProviders_WhenProvidersExist()
    {
        _context.OidcProviders.Add(new OidcProvider("google", "Google", "https://accounts.google.com", "client-id-1"));
        _context.OidcProviders.Add(new OidcProvider("github", "GitHub", "https://github.com", "client-id-2"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.Name == "google");
        result.Should().Contain(p => p.Name == "github");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProvider_WhenProviderExists()
    {
        var provider = new OidcProvider("google", "Google", "https://accounts.google.com", "client-id-1");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetByIdAsync(provider.Id);

        result.Should().NotBeNull();
        result.Name.Should().Be("google");
        result.DisplayName.Should().Be("Google");
    }

    [Fact]
    public async Task GetByIdAsync_ShouldThrowEntityNotFoundException_WhenProviderDoesNotExist()
    {
        var act = async () => await _service.GetByIdAsync(999);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*OidcProvider*999*");
    }

    #endregion

    #region CreateAsync

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnProvider_WhenNameIsUnique()
    {
        var result = await _service.CreateAsync(
            name: "google",
            displayName: "Google",
            authority: "https://accounts.google.com",
            clientId: "client-id",
            clientSecret: "secret",
            scopes: "openid profile email",
            autoProvisionUsers: true,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        result.Should().NotBeNull();
        result.Name.Should().Be("google");
        result.DisplayName.Should().Be("Google");
        result.Authority.Should().Be("https://accounts.google.com");
        result.ClientId.Should().Be("client-id");
        result.ClientSecret.Should().Be("secret");
        result.Scopes.Should().Be("openid profile email");
        result.AutoProvisionUsers.Should().BeTrue();

        var stored = await _context.OidcProviders.FindAsync(new object[] { result.Id }, TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldThrowDomainException_WhenProviderWithSameNameAlreadyExists()
    {
        _context.OidcProviders.Add(new OidcProvider("google", "Google", "https://accounts.google.com", "client-id"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await _service.CreateAsync(
            name: "google",
            displayName: "Google 2",
            authority: "https://accounts.google.com",
            clientId: "client-id-2",
            clientSecret: null,
            scopes: "openid",
            autoProvisionUsers: true,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        await act.Should().ThrowAsync<DomainException>()
            .WithMessage("Only one OIDC provider is supported. Delete the existing provider first.");
    }

    [Fact]
    public async Task CreateAsync_ShouldSetOptionalEndpointOverrides_WhenProvided()
    {
        var result = await _service.CreateAsync(
            name: "custom-oidc",
            displayName: "Custom OIDC",
            authority: "https://idp.example.com",
            clientId: "client-id",
            clientSecret: null,
            scopes: "openid",
            autoProvisionUsers: false,
            authorizationEndpoint: "https://idp.example.com/auth",
            tokenEndpoint: "https://idp.example.com/token",
            userInfoEndpoint: "https://idp.example.com/userinfo",
            usernameClaimType: "preferred_username",
            emailClaimType: "email",
            displayNameClaimType: "name",
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: "https://example.com/icon.png",
            buttonColor: "#ff0000");

        result.AuthorizationEndpoint.Should().Be("https://idp.example.com/auth");
        result.TokenEndpoint.Should().Be("https://idp.example.com/token");
        result.UserInfoEndpoint.Should().Be("https://idp.example.com/userinfo");
        result.UsernameClaimType.Should().Be("preferred_username");
        result.EmailClaimType.Should().Be("email");
        result.DisplayNameClaimType.Should().Be("name");
        result.IconUrl.Should().Be("https://example.com/icon.png");
        result.ButtonColor.Should().Be("#ff0000");
        result.AutoProvisionUsers.Should().BeFalse();
    }

    [Fact]
    public async Task CreateAsync_ShouldSetNullOptionalFields_WhenNotProvided()
    {
        var result = await _service.CreateAsync(
            name: "minimal",
            displayName: "Minimal",
            authority: "https://idp.example.com",
            clientId: "client-id",
            clientSecret: null,
            scopes: "openid",
            autoProvisionUsers: true,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        result.ClientSecret.Should().BeNull();
        result.AuthorizationEndpoint.Should().BeNull();
        result.TokenEndpoint.Should().BeNull();
        result.UserInfoEndpoint.Should().BeNull();
        result.UsernameClaimType.Should().BeNull();
        result.EmailClaimType.Should().BeNull();
        result.DisplayNameClaimType.Should().BeNull();
        result.IconUrl.Should().BeNull();
        result.ButtonColor.Should().BeNull();
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProvider_WhenProviderExists()
    {
        var provider = new OidcProvider("google", "Google", "https://accounts.google.com", "old-client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.UpdateAsync(
            id: provider.Id,
            displayName: "Google Updated",
            authority: "https://accounts.google.com/v2",
            clientId: "new-client-id",
            clientSecret: "new-secret",
            enabled: false,
            scopes: "openid email",
            autoProvisionUsers: false,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        result.Should().NotBeNull();
        result.DisplayName.Should().Be("Google Updated");
        result.Authority.Should().Be("https://accounts.google.com/v2");
        result.ClientId.Should().Be("new-client-id");
        result.ClientSecret.Should().Be("new-secret");
        result.Enabled.Should().BeFalse();
        result.Scopes.Should().Be("openid email");
        result.AutoProvisionUsers.Should().BeFalse();
    }

    [Fact]
    public async Task UpdateAsync_ShouldThrowEntityNotFoundException_WhenProviderDoesNotExist()
    {
        var act = async () => await _service.UpdateAsync(
            id: 999,
            displayName: "Updated",
            authority: "https://idp.example.com",
            clientId: "client-id",
            clientSecret: null,
            enabled: true,
            scopes: "openid",
            autoProvisionUsers: true,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*OidcProvider*999*");
    }

    [Fact]
    public async Task UpdateAsync_ShouldPersistChanges_WhenUpdateSucceeds()
    {
        var provider = new OidcProvider("github", "GitHub", "https://github.com", "old-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.UpdateAsync(
            id: provider.Id,
            displayName: "GitHub Updated",
            authority: "https://github.com",
            clientId: "new-id",
            clientSecret: null,
            enabled: true,
            scopes: "openid",
            autoProvisionUsers: true,
            authorizationEndpoint: null,
            tokenEndpoint: null,
            userInfoEndpoint: null,
            usernameClaimType: null,
            emailClaimType: null,
            displayNameClaimType: null,
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: null,
            buttonColor: null);

        var stored = await _context.OidcProviders.FindAsync(new object[] { provider.Id }, TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.DisplayName.Should().Be("GitHub Updated");
        stored.ClientId.Should().Be("new-id");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOptionalEndpointOverrides_WhenProvided()
    {
        var provider = new OidcProvider("custom", "Custom", "https://idp.example.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.UpdateAsync(
            id: provider.Id,
            displayName: "Custom",
            authority: "https://idp.example.com",
            clientId: "client-id",
            clientSecret: null,
            enabled: true,
            scopes: "openid",
            autoProvisionUsers: true,
            authorizationEndpoint: "https://idp.example.com/auth",
            tokenEndpoint: "https://idp.example.com/token",
            userInfoEndpoint: "https://idp.example.com/userinfo",
            usernameClaimType: "sub",
            emailClaimType: "email",
            displayNameClaimType: "name",
            rolesClaimType: null,
            adminGroupValue: null,
            iconUrl: "https://example.com/icon.png",
            buttonColor: "#0000ff");

        result.AuthorizationEndpoint.Should().Be("https://idp.example.com/auth");
        result.TokenEndpoint.Should().Be("https://idp.example.com/token");
        result.UserInfoEndpoint.Should().Be("https://idp.example.com/userinfo");
        result.UsernameClaimType.Should().Be("sub");
        result.EmailClaimType.Should().Be("email");
        result.DisplayNameClaimType.Should().Be("name");
        result.IconUrl.Should().Be("https://example.com/icon.png");
        result.ButtonColor.Should().Be("#0000ff");
    }

    #endregion

    #region DeleteAsync

    [Fact]
    public async Task DeleteAsync_ShouldRemoveProvider_WhenProviderExists()
    {
        var provider = new OidcProvider("google", "Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider.Id);

        var stored = await _context.OidcProviders.FindAsync(new object[] { provider.Id }, TestContext.Current.CancellationToken);
        stored.Should().BeNull();
    }

    [Fact]
    public async Task DeleteAsync_ShouldThrowEntityNotFoundException_WhenProviderDoesNotExist()
    {
        var act = async () => await _service.DeleteAsync(999);

        await act.Should().ThrowAsync<EntityNotFoundException>()
            .WithMessage("*OidcProvider*999*");
    }

    [Fact]
    public async Task DeleteAsync_ShouldOnlyRemoveTargetProvider_WhenMultipleProvidersExist()
    {
        var provider1 = new OidcProvider("google", "Google", "https://accounts.google.com", "client-id-1");
        var provider2 = new OidcProvider("github", "GitHub", "https://github.com", "client-id-2");
        _context.OidcProviders.AddRange(provider1, provider2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider1.Id);

        var remaining = await _context.OidcProviders.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().HaveCount(1);
        remaining.Should().Contain(p => p.Name == "github");
        remaining.Should().NotContain(p => p.Name == "google");
    }

    #endregion
}
