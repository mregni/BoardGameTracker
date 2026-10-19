using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
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
        "keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, "openid profile email", true,
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
        var provider = new OidcProvider("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
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
        var dto = ok.Value.Should().BeOfType<OidcProviderDto>().Subject;
        dto.Name.Should().Be("keycloak");
        dto.DisplayName.Should().Be("Keycloak SSO");
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
        var provider = new OidcProvider("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync("https://games.example.com");
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
                "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null))
            .ReturnsAsync(provider);
        var controller = CreateController("http", "192.168.1.20:5444");

        var result = await controller.UpdateProvider(1, UpdateRequest(true));

        result.Should().BeOfType<OkObjectResult>();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Once);
        _providerServiceMock.Verify(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
            "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task UpdateProvider_ShouldSaveADisabledProvider_OverPlainHttp()
    {
        var provider = new OidcProvider("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _providerServiceMock
            .Setup(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, false,
                "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null))
            .ReturnsAsync(provider);
        var controller = CreateController("http", "192.168.1.20:5444");

        var result = await controller.UpdateProvider(1, UpdateRequest(false));

        result.Should().BeOfType<OkObjectResult>();
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
        var provider = new OidcProvider("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt");
        _publicUrlBuilderMock.Setup(x => x.GetConfiguredBaseUrlAsync()).ReturnsAsync(publicUrl);
        _providerServiceMock
            .Setup(x => x.CreateAsync("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null,
                "openid profile email", true, null, null, null, null, null, null, null, null, null, null, null, enabled))
            .ReturnsAsync(provider);

        var result = await _controller.CreateProvider(CreateRequest(enabled));

        result.Should().BeOfType<CreatedAtActionResult>();
        _publicUrlBuilderMock.Verify(x => x.GetConfiguredBaseUrlAsync(), Times.Exactly(enabled ? 1 : 0));
        _providerServiceMock.Verify(x => x.CreateAsync("keycloak", "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null,
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
}
