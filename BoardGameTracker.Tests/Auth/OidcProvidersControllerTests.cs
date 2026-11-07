using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcProvidersControllerTests
{
    private readonly Mock<IOidcProviderService> _providerServiceMock;
    private readonly Mock<IOidcService> _oidcServiceMock;
    private readonly Mock<IPublicUrlBuilder> _publicUrlBuilderMock;
    private readonly OidcProvidersController _controller;

    public OidcProvidersControllerTests()
    {
        _providerServiceMock = new Mock<IOidcProviderService>();
        _oidcServiceMock = new Mock<IOidcService>();
        _publicUrlBuilderMock = new Mock<IPublicUrlBuilder>();
        _controller = CreateController("https", "games.example.com");
    }

    private OidcProvidersController CreateController(string scheme, string host)
    {
        var context = new DefaultHttpContext();
        context.Request.Scheme = scheme;
        context.Request.Host = new HostString(host);
        return new OidcProvidersController(_providerServiceMock.Object, _oidcServiceMock.Object, _publicUrlBuilderMock.Object)
        {
            ControllerContext = new ControllerContext { HttpContext = context },
        };
    }

    private static UpdateOidcProviderRequest UpdateRequest(bool enabled) => new(
        1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, enabled, "openid profile email", false,
        null, null, null, null, null, null, "groups", "bgt-admins", null, null);

    private static CreateOidcProviderRequest CreateRequest(bool enabled) => new(
        "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, "openid profile email", true,
        null, null, null, null, null, null, null, null, null, null, null, enabled);

    private void VerifyNoOtherCalls()
    {
        _providerServiceMock.VerifyNoOtherCalls();
        _oidcServiceMock.VerifyNoOtherCalls();
        _publicUrlBuilderMock.VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldReturnOkWithTheUpdatedProvider()
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        var request = new UpdateOidcProviderRequest(
            1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true, "openid profile email", false,
            null, null, null, null, null, null, "groups", "bgt-admins", null, null);
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
                "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null))
            .ReturnsAsync(provider);
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);

        var result = await _controller.UpdateProvider(1, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OidcProviderDto>().Which;
        dto.DisplayName.Should().Be("Keycloak SSO");
        dto.Authority.Should().Be("https://idp.example.com/realms/bgt");
        dto.ClientId.Should().Be("bgt");
        _providerServiceMock.Verify(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
            "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null), Times.Once);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://games.example.com")]
    [InlineData("http://192.168.1.20:5444")]
    public async Task UpdateProvider_ShouldRefuseToEnable_WhenThePublicUrlIsPlainHttp(string publicUrl)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);

        var act = () => _controller.UpdateProvider(1, UpdateRequest(true));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Be(Constants.Errors.OidcHttpsRequired);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldRefuseToEnable_OverPlainHttp_WithoutAPublicUrl()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);
        var controller = CreateController("http", "192.168.1.20:5444");

        var act = () => controller.UpdateProvider(1, UpdateRequest(true));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Be(Constants.Errors.OidcHttpsRequired);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldAllowEnabling_WhenThePublicUrlIsHttps_EvenOverALanHttpRequest()
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://games.example.com");
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
                "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null))
            .ReturnsAsync(provider);
        var controller = CreateController("http", "192.168.1.20:5444");

        var result = await controller.UpdateProvider(1, UpdateRequest(true));

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { DisplayName = "Keycloak SSO", Authority = "https://idp.example.com/realms/bgt", ClientId = "bgt" });
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _providerServiceMock.Verify(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
            "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldSaveADisabledProvider_OverPlainHttp()
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, false,
                "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null))
            .ReturnsAsync(provider);
        var controller = CreateController("http", "192.168.1.20:5444");

        var result = await controller.UpdateProvider(1, UpdateRequest(false));

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { DisplayName = "Keycloak SSO", Authority = "https://idp.example.com/realms/bgt", ClientId = "bgt" });
        _providerServiceMock.Verify(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, false,
            "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateProvider_ShouldRefuseAnEnabledProvider_OverPlainHttp()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("http://games.example.com");

        var act = () => _controller.CreateProvider(CreateRequest(true));

        (await act.Should().ThrowAsync<ValidationException>()).Which.Message.Should().Be(Constants.Errors.OidcHttpsRequired);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(false, "http://games.example.com")]
    [InlineData(true, "https://games.example.com")]
    [InlineData(true, "http://localhost:5444")]
    public async Task CreateProvider_ShouldPassTheEnabledFlag_WhenItIsAllowed(bool enabled, string publicUrl)
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        _providerServiceMock
            .Setup(x => x.CreateAsync("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null,
                "openid profile email", true, null, null, null, null, null, null, null, null, null, null, null, enabled))
            .ReturnsAsync(provider);

        var result = await _controller.CreateProvider(CreateRequest(enabled));

        var created = result.Should().BeOfType<CreatedAtActionResult>().Subject;
        created.ActionName.Should().Be(nameof(OidcProvidersController.GetProvider));
        created.RouteValues.Should().ContainKey("id").WhoseValue.Should().Be(provider.Id);
        var dto = created.Value.Should().BeOfType<OidcProviderDto>().Which;
        dto.DisplayName.Should().Be("Keycloak SSO");
        dto.Authority.Should().Be("https://idp.example.com/realms/bgt");
        dto.ClientId.Should().Be("bgt");
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Exactly(enabled ? 1 : 0));
        _providerServiceMock.Verify(x => x.CreateAsync("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null,
            "openid profile email", true, null, null, null, null, null, null, null, null, null, null, null, enabled), Times.Once);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("http://games.example.com", "https", "games.example.com", true)]
    [InlineData("https://games.example.com", "http", "192.168.1.20", false)]
    [InlineData(null, "http", "192.168.1.20:5444", true)]
    [InlineData(null, "https", "games.example.com", false)]
    [InlineData(null, "http", "localhost:5444", false)]
    public async Task GetSetup_ShouldReportWhetherHttpsIsRequired(string? publicUrl, string scheme, string host, bool expected)
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        var controller = CreateController(scheme, host);

        var result = await controller.GetSetup();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeOfType<OidcSetupDto>().Which.HttpsRequired.Should().Be(expected);
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task CreateProvider_ShouldPassEveryFieldInItsOwnPlace()
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        var request = new CreateOidcProviderRequest("name", "authority", "client", "secret", "scopes", true,
            "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", false, "picture");
        _providerServiceMock
            .Setup(x => x.CreateAsync("name", "authority", "client", "secret", "scopes", true,
                "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", false, "picture"))
            .ReturnsAsync(provider);

        await _controller.CreateProvider(request);

        _providerServiceMock.Verify(x => x.CreateAsync("name", "authority", "client", "secret", "scopes", true,
            "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", false, "picture"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldPassEveryFieldInItsOwnPlace()
    {
        var provider = new OidcProvider("Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        var request = new UpdateOidcProviderRequest(7, "name", "authority", "client", "secret", false, "scopes", true,
            "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", "picture");
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "name", "authority", "client", "secret", false, "scopes", true,
                "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", "picture"))
            .ReturnsAsync(provider);

        await _controller.UpdateProvider(1, request);

        _providerServiceMock.Verify(x => x.UpdateAsync(1, "name", "authority", "client", "secret", false, "scopes", true,
            "authorize", "token", "userinfo", "username", "email", "display", "roles", "admins", "icon", "color", "Reader", "picture"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSetup_ShouldBuildTheAddressesFromTheConfiguredPublicUrl()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://public.example.com/");

        var result = await _controller.GetSetup();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcSetupDto(
            "https://public.example.com/",
            true,
            "https://public.example.com/api/auth/oidc/callback",
            "https://public.example.com/api/auth/oidc/link-callback",
            "https://public.example.com/login",
            false));
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetSetup_ShouldFallBackToTheRequestAddress_WithoutAPublicUrl()
    {
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync((string?)null);

        var result = await _controller.GetSetup();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().Be(new OidcSetupDto(
            "https://games.example.com",
            false,
            "https://games.example.com/api/auth/oidc/callback",
            "https://games.example.com/api/auth/oidc/link-callback",
            "https://games.example.com/login",
            false));
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task TestDiscovery_ShouldReturnTheResultOfTheService()
    {
        var discovery = new OidcDiscoveryResultDto("issuer", "authorize", "token", null, null, true, ["RS256"], true, null);
        _oidcServiceMock.Setup(x => x.TestDiscoveryAsync("https://idp.example.com")).ReturnsAsync(discovery);

        var result = await _controller.TestDiscovery(new TestOidcDiscoveryRequest("https://idp.example.com"));

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeSameAs(discovery);
        _oidcServiceMock.Verify(x => x.TestDiscoveryAsync("https://idp.example.com"), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProviders_ShouldReturnTheListItems()
    {
        var first = new OidcProvider("First", "https://first.example.com", "one");
        var second = new OidcProvider("Second", "https://second.example.com", "two");
        _providerServiceMock.Setup(x => x.GetAllAsync()).ReturnsAsync([first, second]);

        var result = await _controller.GetProviders();

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeAssignableTo<IEnumerable<OidcProviderListDto>>()
            .Which.Should().Equal(first.ToListDto(), second.ToListDto());
        _providerServiceMock.Verify(x => x.GetAllAsync(), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task GetProvider_ShouldReturnTheProviderWithTheId()
    {
        var provider = new OidcProvider("First", "https://first.example.com", "one");
        _providerServiceMock.Setup(x => x.GetByIdAsync(5)).ReturnsAsync(provider);

        var result = await _controller.GetProvider(5);

        result.Should().BeOfType<OkObjectResult>().Which.Value.Should().BeEquivalentTo(new { DisplayName = "First", Authority = "https://first.example.com", ClientId = "one" });
        _providerServiceMock.Verify(x => x.GetByIdAsync(5), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task DeleteProvider_ShouldDeleteTheProviderWithTheId()
    {
        var result = await _controller.DeleteProvider(5);

        result.Should().BeOfType<NoContentResult>();
        _providerServiceMock.Verify(x => x.DeleteAsync(5), Times.Once);
        VerifyNoOtherCalls();
    }
}
