using System.Collections.Generic;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcConfigurationProviderTests : IDisposable
{
    private const string Authority = "https://idp.example.com/realms/bgt";

    private readonly MetadataHandler _handler = new();
    private readonly Mock<IHttpClientFactory> _httpClientFactoryMock = new();
    private readonly TestIdTokens _keys = new();

    public OidcConfigurationProviderTests()
    {
        _httpClientFactoryMock.Setup(x => x.CreateClient(OidcService.HttpClientName)).Returns(() => new HttpClient(_handler, disposeHandler: false));
        _handler.Documents[$"{Authority}/.well-known/openid-configuration"] = JsonSerializer.Serialize(new
        {
            issuer = Authority,
            authorization_endpoint = $"{Authority}/auth",
            token_endpoint = $"{Authority}/token",
            end_session_endpoint = $"{Authority}/logout",
            jwks_uri = $"{Authority}/certs",
        });
        _handler.Documents[$"{Authority}/certs"] = _keys.JwksJson;
    }

    public void Dispose()
    {
        _handler.Dispose();
        _keys.Dispose();
        GC.SuppressFinalize(this);
    }

    private OidcConfigurationProvider CreateProvider(TimeSpan? minimumRefreshInterval = null) =>
        new(_httpClientFactoryMock.Object, Mock.Of<ILogger<OidcConfigurationProvider>>())
        {
            MinimumRefreshInterval = minimumRefreshInterval ?? TimeSpan.FromSeconds(30),
        };

    [Fact]
    public async Task GetAsync_ShouldLoadTheDiscoveryDocumentAndItsSigningKeys()
    {
        var provider = CreateProvider();

        var configuration = await provider.GetAsync(Authority + "/");

        configuration.Issuer.Should().Be(Authority);
        configuration.EndSessionEndpoint.Should().Be($"{Authority}/logout");
        configuration.SigningKeys.Should().ContainSingle(k => k.KeyId == _keys.KeyId);
    }

    [Fact]
    public async Task GetAsync_ShouldCacheTheConfiguration()
    {
        var provider = CreateProvider();

        await provider.GetAsync(Authority);
        await provider.GetAsync(Authority);

        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task RefreshAsync_ShouldFetchTheDocumentsAgain_OnceTheMinimumIntervalHasPassed()
    {
        var provider = CreateProvider(TimeSpan.Zero);
        await provider.GetAsync(Authority);
        using var rotated = new TestIdTokens("key-2");
        _handler.Documents[$"{Authority}/certs"] = rotated.JwksJson;

        var configuration = await provider.RefreshAsync(Authority);

        configuration.SigningKeys.Should().ContainSingle(k => k.KeyId == "key-2");
        _handler.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task RefreshAsync_ShouldNotFetchAgain_WithinTheMinimumInterval()
    {
        var provider = CreateProvider(TimeSpan.FromMinutes(5));
        await provider.GetAsync(Authority);

        await provider.RefreshAsync(Authority);

        _handler.Requests.Should().HaveCount(2);
    }

    [Fact]
    public async Task Invalidate_ShouldForceTheNextCallToFetchAgain()
    {
        var provider = CreateProvider();
        await provider.GetAsync(Authority);

        provider.Invalidate(Authority);
        await provider.GetAsync(Authority);

        _handler.Requests.Should().HaveCount(4);
    }

    [Fact]
    public async Task GetAsync_ShouldReportAnUnreachableProvider_AndRetryOnTheNextCall()
    {
        var provider = CreateProvider();
        _handler.Fail = true;

        var act = () => provider.GetAsync(Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
        _handler.Fail = false;
        (await provider.GetAsync(Authority)).Issuer.Should().Be(Authority);
    }

    [Fact]
    public async Task GetAsync_ShouldRefuseAnInsecureKeySetAddress()
    {
        var provider = CreateProvider();
        _handler.Documents[$"{Authority}/.well-known/openid-configuration"] = JsonSerializer.Serialize(new
        {
            issuer = Authority,
            authorization_endpoint = $"{Authority}/auth",
            token_endpoint = $"{Authority}/token",
            jwks_uri = "http://keys.example.com/certs",
        });

        var act = () => provider.GetAsync(Authority);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcDiscoveryFailed);
        _handler.Requests.Should().NotContain("http://keys.example.com/certs");
    }

    private sealed class MetadataHandler : HttpMessageHandler
    {
        public Dictionary<string, string> Documents { get; } = new();

        public List<string> Requests { get; } = [];

        public bool Fail { get; set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var address = request.RequestUri!.ToString();
            Requests.Add(address);
            if (Fail || !Documents.TryGetValue(address, out var document))
            {
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.NotFound));
            }

            return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(document, Encoding.UTF8, "application/json"),
            });
        }
    }
}
