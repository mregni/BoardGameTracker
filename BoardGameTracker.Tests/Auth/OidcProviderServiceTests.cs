using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using FluentAssertions;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcProviderServiceTests : IDisposable
{
    private readonly MainDbContext _context;
    private readonly Mock<IOidcConfigurationProvider> _configurationsMock = new();
    private readonly Mock<ILogger<OidcProviderService>> _loggerMock;
    private readonly SecretProtector _secretProtector;
    private readonly OidcProviderService _service;

    public OidcProviderServiceTests()
    {
        var options = new DbContextOptionsBuilder<MainDbContext>()
            .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
            .Options;
        _context = new MainDbContext(options);

        _loggerMock = new Mock<ILogger<OidcProviderService>>();
        _secretProtector = new SecretProtector(new EphemeralDataProtectionProvider(), Mock.Of<ILogger<SecretProtector>>());
        _service = new OidcProviderService(_context, _configurationsMock.Object, _secretProtector, _loggerMock.Object);
    }

    public void Dispose()
    {
        _context.Dispose();
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
        _context.OidcProviders.Add(new OidcProvider("Google", "https://accounts.google.com", "client-id-1"));
        _context.OidcProviders.Add(new OidcProvider("GitHub", "https://github.com", "client-id-2"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetAllAsync();

        result.Should().HaveCount(2);
        result.Should().Contain(p => p.DisplayName == "Google");
        result.Should().Contain(p => p.DisplayName == "GitHub");
    }

    #endregion

    #region GetByIdAsync

    [Fact]
    public async Task GetByIdAsync_ShouldReturnProvider_WhenProviderExists()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id-1");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.GetByIdAsync(provider.Id);

        result.Should().NotBeNull();
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

    [Theory]
    [InlineData("http://auth.example.com")]
    [InlineData("ftp://accounts.google.com")]
    public async Task CreateAsync_ShouldRejectAnInsecurePublicAuthority(string authority)
    {
        var act = () => _service.CreateAsync(
            displayName: "SSO",
            authority: authority,
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

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InsecureAuthority);
        (await _context.OidcProviders.CountAsync()).Should().Be(0);
    }

    [Fact]
    public async Task CreateAsync_ShouldCreateAndReturnProvider_WhenNoneExists()
    {
        var result = await _service.CreateAsync(
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
        result.DisplayName.Should().Be("Google");
        result.Authority.Should().Be("https://accounts.google.com");
        result.ClientId.Should().Be("client-id");
        result.ClientSecret.Should().NotBe("secret");
        _secretProtector.Unprotect(result.ClientSecret!).Should().Be("secret");
        result.Scopes.Should().Be("openid profile email");
        result.AutoProvisionUsers.Should().BeTrue();

        var stored = await _context.OidcProviders.FindAsync([result.Id], TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldInvalidateTheCachedDiscoveryDocument()
    {
        await _service.CreateAsync("Authentik", "https://sso.example.com/application/o/bgt/", "client-id",
            null, "openid profile email", true, null, null, null, null, null, null, null, null, null, null);

        _configurationsMock.Verify(x => x.Invalidate("https://sso.example.com/application/o/bgt/"), Times.Once);
        _configurationsMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateAsync_ShouldRefuseASecondProvider()
    {
        _context.OidcProviders.Add(new OidcProvider("Google", "https://accounts.google.com", "client-id"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var act = async () => await _service.CreateAsync(
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

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcProviderExists);
        (await _context.OidcProviders.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldSetOptionalEndpointOverrides_WhenProvided()
    {
        var result = await _service.CreateAsync(
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
    public async Task CreateAsync_ShouldSetNullOptionalFields_AndTheDefaultClaimMapping_WhenNotProvided()
    {
        var result = await _service.CreateAsync(
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
        result.UsernameClaimType.Should().Be("preferred_username");
        result.EmailClaimType.Should().Be("email");
        result.DisplayNameClaimType.Should().Be("name");
        result.RolesClaimType.Should().Be("groups");
        result.AdminGroupValue.Should().Be("bgt-admins");
        result.PictureClaimType.Should().Be("picture");
        result.IconUrl.Should().BeNull();
        result.ButtonColor.Should().BeNull();
    }

    [Fact]
    public async Task CreateAsync_ShouldStoreTheDefaultClaimMapping_ForWhitespaceValues_AndKeepConfiguredOnes()
    {
        var result = await _service.CreateAsync("Minimal", "https://idp.example.com", "client-id", null, "openid", true,
            null, null, null, " ", "", " mail ", "roles", "  ", null, null, pictureClaimType: " ");

        result.UsernameClaimType.Should().Be("preferred_username");
        result.EmailClaimType.Should().Be("email");
        result.DisplayNameClaimType.Should().Be("mail");
        result.RolesClaimType.Should().Be("roles");
        result.AdminGroupValue.Should().Be("bgt-admins");
        result.PictureClaimType.Should().Be("picture");
    }

    #endregion

    #region UpdateAsync

    [Fact]
    public async Task UpdateAsync_ShouldUpdateProvider_WhenProviderExists()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "old-client-id");
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
        result.ClientSecret.Should().NotBe("new-secret");
        _secretProtector.Unprotect(result.ClientSecret!).Should().Be("new-secret");
        result.Enabled.Should().BeFalse();
        result.Scopes.Should().Be("openid email");
        result.AutoProvisionUsers.Should().BeFalse();

        var stored = await _context.OidcProviders.FindAsync([provider.Id], TestContext.Current.CancellationToken);
        stored.Should().NotBeNull();
        stored!.DisplayName.Should().Be("Google Updated");
        stored.ClientId.Should().Be("new-client-id");
    }

    [Fact]
    public async Task UpdateAsync_ShouldKeepTheStoredSecret_WhenNoSecretIsSent()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await UpdateSecretAsync(provider.Id, "stored-secret");

        var result = await UpdateSecretAsync(provider.Id, null);

        _secretProtector.Unprotect(result.ClientSecret!).Should().Be("stored-secret");
    }

    [Fact]
    public async Task UpdateAsync_ShouldClearTheSecret_WhenAnEmptySecretIsSent()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        await UpdateSecretAsync(provider.Id, "stored-secret");

        var result = await UpdateSecretAsync(provider.Id, string.Empty);

        result.ClientSecret.Should().BeEmpty();
    }

    private Task<OidcProvider> UpdateSecretAsync(int id, string? clientSecret) => _service.UpdateAsync(
        id: id,
        displayName: "Google",
        authority: "https://accounts.google.com",
        clientId: "client-id",
        clientSecret: clientSecret,
        enabled: true,
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
    public async Task UpdateAsync_ShouldInvalidateCachedDiscoveryDocument()
    {
        var provider = new OidcProvider("GitHub", "https://github.com", "old-id");
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

        _configurationsMock.Verify(x => x.Invalidate("https://github.com"), Times.Exactly(2));
    }

    [Fact]
    public async Task UpdateAsync_ShouldUpdateOptionalEndpointOverrides_WhenProvided()
    {
        var provider = new OidcProvider("Custom", "https://idp.example.com", "client-id");
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
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider.Id);

        var stored = await _context.OidcProviders.FindAsync([provider.Id], TestContext.Current.CancellationToken);
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
        var provider1 = new OidcProvider("Google", "https://accounts.google.com", "client-id-1");
        var provider2 = new OidcProvider("GitHub", "https://github.com", "client-id-2");
        _context.OidcProviders.AddRange(provider1, provider2);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider1.Id);

        var remaining = await _context.OidcProviders.ToListAsync(TestContext.Current.CancellationToken);
        remaining.Should().HaveCount(1);
        remaining.Should().Contain(p => p.DisplayName == "GitHub");
        remaining.Should().NotContain(p => p.DisplayName == "Google");
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveAssociatedExternalLogins()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1"));
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "google", "https://accounts.google.com", "key-2"));
        _context.ExternalLogins.Add(new ExternalLogin("user-3", "other", "https://other.example.com", "key-3"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider.Id);

        var remainingLogins = await _context.ExternalLogins.ToListAsync(TestContext.Current.CancellationToken);
        remainingLogins.Should().ContainSingle(x => x.Provider == "other");
    }

    [Fact]
    public async Task UpdateAsync_ShouldUnlinkEveryAccount_WhenTheAuthorityPointsElsewhere()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1"));
        _context.ExternalLogins.Add(new ExternalLogin("user-3", "other", "https://other.example.com", "key-3"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await UpdateAuthorityAsync(provider.Id, "https://sso.example.com/realms/games");

        var remainingLogins = await _context.ExternalLogins.ToListAsync(TestContext.Current.CancellationToken);
        remainingLogins.Should().ContainSingle(x => x.Provider == "other");
    }

    [Theory]
    [InlineData("https://accounts.google.com")]
    [InlineData("https://accounts.google.com/")]
    [InlineData("HTTPS://Accounts.Google.com")]
    public async Task UpdateAsync_ShouldKeepTheLinks_WhenTheAuthorityIsUnchanged(string authority)
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "google", "https://accounts.google.com", "key-1"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await UpdateAuthorityAsync(provider.Id, authority);

        (await _context.ExternalLogins.CountAsync(TestContext.Current.CancellationToken)).Should().Be(1);
    }

    private Task<OidcProvider> UpdateAuthorityAsync(int id, string authority) => _service.UpdateAsync(
        id: id,
        displayName: "Google",
        authority: authority,
        clientId: "client-id",
        clientSecret: null,
        enabled: true,
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

    [Fact]
    public async Task DeleteAsync_ShouldInvalidateCachedDiscoveryDocument()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider.Id);

        _configurationsMock.Verify(x => x.Invalidate("https://accounts.google.com"), Times.Once);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRelabelItsLinks_WhenTheDisplayNameChanges()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "Google", "https://accounts.google.com", "key-1"));
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "google", null, "key-2"));
        _context.ExternalLogins.Add(new ExternalLogin("user-3", "other", "https://other.example.com", "key-3"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        var result = await _service.UpdateAsync(provider.Id, "Company login", "https://accounts.google.com", "client-id", null, true,
            "openid", true, null, null, null, null, null, null, null, null, null, null);

        result.DisplayName.Should().Be("Company login");
        var logins = await _context.ExternalLogins.OrderBy(x => x.UserId).ToListAsync(TestContext.Current.CancellationToken);
        logins.Select(x => x.Provider).Should().Equal("Company login", "google", "other");
        logins[0].Issuer.Should().Be("https://accounts.google.com");
    }

    [Fact]
    public async Task DeleteAsync_ShouldLeaveLinksWithoutAnIssuer()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        _context.ExternalLogins.Add(new ExternalLogin("user-1", "Google", "https://accounts.google.com", "key-1"));
        _context.ExternalLogins.Add(new ExternalLogin("user-2", "deleted-provider", null, "key-2"));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.DeleteAsync(provider.Id);

        (await _context.ExternalLogins.SingleAsync(TestContext.Current.CancellationToken)).UserId.Should().Be("user-2");
    }

    [Fact]
    public async Task UpdateAsync_ShouldDropTheOidcSessions_WhenTheAuthorityPointsElsewhere()
    {
        var provider = new OidcProvider("Google", "https://accounts.google.com", "client-id");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);
        _context.OidcSessions.Add(new OidcSession("hash", "user-1", provider.Id, "token", DateTime.UtcNow.AddDays(1)));
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await UpdateAuthorityAsync(provider.Id, "https://sso.example.com/realms/games");

        (await _context.OidcSessions.CountAsync(TestContext.Current.CancellationToken)).Should().Be(0);
        _configurationsMock.Verify(x => x.Invalidate("https://accounts.google.com"), Times.Once);
        _configurationsMock.Verify(x => x.Invalidate("https://sso.example.com/realms/games"), Times.Once);
    }

    [Theory]
    [InlineData("Reader", "Reader")]
    [InlineData(null, "User")]
    public async Task CreateAsync_ShouldStoreTheDefaultRole(string? defaultRole, string expected)
    {
        var result = await _service.CreateAsync("Keycloak", "https://sso.example.com", "bgt", null, "openid", true,
            null, null, null, null, null, null, null, null, null, null, defaultRole);

        result.DefaultRole.Should().Be(expected);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task CreateAsync_ShouldStoreTheEnabledFlag(bool enabled)
    {
        var result = await _service.CreateAsync("Keycloak", "https://sso.example.com", "bgt", null, "openid", true,
            null, null, null, null, null, null, null, null, null, null, null, enabled);

        result.Enabled.Should().Be(enabled);
        (await _context.OidcProviders.SingleAsync(TestContext.Current.CancellationToken)).Enabled.Should().Be(enabled);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Owner")]
    public async Task CreateAsync_ShouldRejectADefaultRoleOtherThanUserOrReader(string defaultRole)
    {
        var act = () => _service.CreateAsync("Keycloak", "https://sso.example.com", "bgt", null, "openid", true,
            null, null, null, null, null, null, null, null, null, null, defaultRole);

        await act.Should().ThrowAsync<ValidationException>().WithMessage(Constants.Errors.InvalidRole);
    }

    [Fact]
    public async Task UpdateAsync_ShouldChangeTheDefaultRole_OnlyWhenOneIsSent()
    {
        var provider = new OidcProvider("Keycloak", "https://sso.example.com", "bgt");
        _context.OidcProviders.Add(provider);
        await _context.SaveChangesAsync(TestContext.Current.CancellationToken);

        await _service.UpdateAsync(provider.Id, "Keycloak", "https://sso.example.com", "bgt", null, true, "openid", true,
            null, null, null, null, null, null, null, null, null, null, "Reader");
        var unchanged = await _service.UpdateAsync(provider.Id, "Keycloak", "https://sso.example.com", "bgt", null, true, "openid", true,
            null, null, null, null, null, null, null, null, null, null);

        unchanged.DefaultRole.Should().Be("Reader");
    }

    #endregion
}
