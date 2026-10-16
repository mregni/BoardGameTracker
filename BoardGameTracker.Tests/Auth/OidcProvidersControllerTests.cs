using BoardGameTracker.Api.Controllers.Admin;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Email.Interfaces;
using FluentAssertions;
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
        _controller = new OidcProvidersController(_providerServiceMock.Object, _oidcServiceMock.Object, _publicUrlBuilderMock.Object);
    }

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

        var result = await _controller.UpdateProvider(1, request);

        var ok = result.Should().BeOfType<OkObjectResult>().Subject;
        var dto = ok.Value.Should().BeOfType<OidcProviderDto>().Subject;
        dto.Name.Should().Be("keycloak");
        dto.DisplayName.Should().Be("Keycloak SSO");
        _providerServiceMock.Verify(x => x.UpdateAsync(1, "Keycloak SSO", "https://idp.example.com/realms/bgt", "bgt", null, true,
            "openid profile email", false, null, null, null, null, null, null, "groups", "bgt-admins", null, null), Times.Once);
        VerifyNoOtherCalls();
    }
}
