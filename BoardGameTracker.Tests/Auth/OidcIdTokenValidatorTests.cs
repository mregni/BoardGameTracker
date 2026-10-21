using System.Threading.Tasks;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using Microsoft.IdentityModel.Tokens;
using Moq;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcIdTokenValidatorTests : IDisposable
{
    private const string Authority = "https://idp.example.com/realms/bgt";
    private const string ClientId = "bgt-client";
    private const string Nonce = "expected-nonce";

    private readonly Mock<IOidcConfigurationProvider> _configurationsMock = new();
    private readonly Mock<ILogger<OidcIdTokenValidator>> _loggerMock = new();
    private readonly TestIdTokens _keys = new();
    private readonly OidcIdTokenValidator _validator;

    public OidcIdTokenValidatorTests()
    {
        _validator = new OidcIdTokenValidator(_configurationsMock.Object, _loggerMock.Object);
    }

    public void Dispose()
    {
        _keys.Dispose();
        GC.SuppressFinalize(this);
    }

    private void VerifyNoOtherCalls()
    {
        _configurationsMock.VerifyNoOtherCalls();
    }

    private static OpenIdConnectConfiguration Configuration(params SecurityKey[] keys)
    {
        var configuration = new OpenIdConnectConfiguration { Issuer = Authority };
        foreach (var key in keys)
        {
            configuration.SigningKeys.Add(key);
        }

        return configuration;
    }

    private void SetupRefresh(OpenIdConnectConfiguration configuration)
    {
        _configurationsMock.Setup(x => x.RefreshAsync(Authority, default)).ReturnsAsync(configuration);
    }

    private void VerifyRefreshedOnce()
    {
        _configurationsMock.Verify(x => x.RefreshAsync(Authority, default), Times.Once);
        VerifyNoOtherCalls();
    }

    private static OpenIdConnectConfiguration Advertising(string algorithm, params SecurityKey[] keys)
    {
        var configuration = Configuration(keys);
        configuration.IdTokenSigningAlgValuesSupported.Add(algorithm);
        return configuration;
    }

    private static Dictionary<string, object> ValidClaims() => TestIdTokens.Claims(Authority, ClientId, "user-sub", Nonce);

    [Fact]
    public async Task ValidateAsync_ShouldReturnTheSubjectAndClaims_ForAValidToken()
    {
        var claims = ValidClaims();
        claims["preferred_username"] = "jane";
        claims["groups"] = new[] { "Admins", "Staff" };

        var result = await _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        result.Subject.Should().Be("user-sub");
        result.Claims.GetValue("preferred_username").Should().Be("jane");
        result.Claims.GetValues("groups").Should().Equal("Admins", "Staff");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldAcceptAnAuthorizedPartyThatIsTheClient_WhenThereAreSeveralAudiences()
    {
        var claims = ValidClaims();
        claims["aud"] = new[] { ClientId, "account" };
        claims["azp"] = ClientId;

        var result = await _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        result.Subject.Should().Be("user-sub");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_ATokenSignedByAnotherKeyWithTheSameKid()
    {
        using var attacker = new TestIdTokens(_keys.KeyId);
        SetupRefresh(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, attacker.Sign(ValidClaims()), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_ATamperedPayload()
    {
        var token = _keys.Sign(ValidClaims());
        var parts = token.Split('.');
        var tamperedClaims = ValidClaims();
        tamperedClaims["sub"] = "someone-else";
        var tamperedPayload = _keys.Sign(tamperedClaims).Split('.')[1];
        SetupRefresh(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, $"{parts[0]}.{tamperedPayload}.{parts[2]}", Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldRefetchTheKeys_WhenTheKidIsUnknown()
    {
        using var rotated = new TestIdTokens("key-2");
        _configurationsMock.Setup(x => x.RefreshAsync(Authority, default)).ReturnsAsync(Configuration(_keys.PublicKey, rotated.PublicKey));

        var result = await _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, rotated.Sign(ValidClaims()), Nonce);

        result.Subject.Should().Be("user-sub");
        _configurationsMock.Verify(x => x.RefreshAsync(Authority, default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_WhenTheKidIsStillUnknownAfterARefresh()
    {
        using var stranger = new TestIdTokens("key-unknown");
        _configurationsMock.Setup(x => x.RefreshAsync(Authority, default)).ReturnsAsync(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, stranger.Sign(ValidClaims()), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _configurationsMock.Verify(x => x.RefreshAsync(Authority, default), Times.Once);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AWrongIssuer()
    {
        var claims = ValidClaims();
        claims["iss"] = "https://evil.example.com";

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AWrongAudience()
    {
        var claims = ValidClaims();
        claims["aud"] = "another-client";

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_SeveralAudiencesWithoutTheClientAsAuthorizedParty()
    {
        var claims = ValidClaims();
        claims["aud"] = new[] { ClientId, "another-client" };

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AnAuthorizedPartyThatIsNotTheClient()
    {
        var claims = ValidClaims();
        claims["azp"] = "another-client";

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData("another-nonce")]
    [InlineData("")]
    [InlineData(null)]
    public async Task ValidateAsync_ShouldReject_ANonceThatIsNotTheOneOfThisSignIn(string? nonce)
    {
        var claims = ValidClaims();
        if (nonce == null)
        {
            claims.Remove("nonce");
        }
        else
        {
            claims["nonce"] = nonce;
        }

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AnExpiredToken()
    {
        var claims = ValidClaims();
        var past = DateTimeOffset.UtcNow.AddMinutes(-10).ToUnixTimeSeconds();
        claims["iat"] = past - 300;
        claims["nbf"] = past - 300;
        claims["exp"] = past;

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldAcceptATokenThatExpiredWithinTheClockSkew()
    {
        var claims = ValidClaims();
        claims["exp"] = DateTimeOffset.UtcNow.AddSeconds(-30).ToUnixTimeSeconds();
        claims["iat"] = DateTimeOffset.UtcNow.AddMinutes(-5).ToUnixTimeSeconds();
        claims["nbf"] = claims["iat"];

        var result = await _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        result.Subject.Should().Be("user-sub");
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_ATokenWithoutAnExpiry()
    {
        var claims = ValidClaims();
        claims.Remove("exp");

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_ATokenThatIsNotYetValid()
    {
        var claims = ValidClaims();
        claims["nbf"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        claims["exp"] = DateTimeOffset.UtcNow.AddMinutes(20).ToUnixTimeSeconds();

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidateAsync_ShouldReject_AMissingOrFutureIssuedAt(bool missing)
    {
        var claims = ValidClaims();
        if (missing)
        {
            claims.Remove("iat");
        }
        else
        {
            claims["iat"] = DateTimeOffset.UtcNow.AddMinutes(10).ToUnixTimeSeconds();
        }

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task ValidateAsync_ShouldReject_AMissingOrEmptySubject(bool missing)
    {
        var claims = ValidClaims();
        if (missing)
        {
            claims.Remove("sub");
        }
        else
        {
            claims["sub"] = "";
        }

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_ASymmetricAlgorithm()
    {
        SetupRefresh(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, TestIdTokens.SignWithSymmetricKey(ValidClaims()), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AnAlgorithmOutsideTheAllowList()
    {
        SetupRefresh(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, _keys.Sign(ValidClaims(), SecurityAlgorithms.RsaSha384), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_AnUnsignedToken()
    {
        var token = _keys.Sign(ValidClaims());
        var parts = token.Split('.');
        var header = Base64UrlEncoder.Encode("{\"alg\":\"none\",\"typ\":\"JWT\"}");

        SetupRefresh(Configuration(_keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Configuration(_keys.PublicKey), ClientId, $"{header}.{parts[1]}.", Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        _configurationsMock.Verify(x => x.RefreshAsync(Authority, default), Times.AtMostOnce);
        _configurationsMock.Invocations.Clear();
        VerifyNoOtherCalls();
    }

    [Fact]
    public void SupportedAlgorithms_ShouldIntersectTheAdvertisedAlgorithmsWithTheAllowList()
    {
        var configuration = new OpenIdConnectConfiguration();
        OidcIdTokenValidator.SupportedAlgorithms(configuration).Should().BeEquivalentTo("RS256", "ES256");

        configuration.IdTokenSigningAlgValuesSupported.Add("RS256");
        configuration.IdTokenSigningAlgValuesSupported.Add("HS256");
        configuration.IdTokenSigningAlgValuesSupported.Add("none");
        OidcIdTokenValidator.SupportedAlgorithms(configuration).Should().Equal("RS256");
    }

    [Fact]
    public async Task ValidateAsync_ShouldReject_WhenTheProviderStillOnlyAdvertisesHs256AfterOneRefresh()
    {
        SetupRefresh(Advertising("HS256", _keys.PublicKey));

        var act = () => _validator.ValidateAsync(Authority, Advertising("HS256", _keys.PublicKey), ClientId, _keys.Sign(ValidClaims()), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldRefreshAStaleHs256Configuration_AndAcceptTheTokenOnceTheProviderSignsWithRs256()
    {
        SetupRefresh(Advertising("RS256", _keys.PublicKey));

        var result = await _validator.ValidateAsync(Authority, Advertising("HS256"), ClientId, _keys.Sign(ValidClaims()), Nonce);

        result.Subject.Should().Be("user-sub");
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldRefreshOnce_WhenTheCachedKeyWithTheSameKidNoLongerMatches()
    {
        using var fresh = new TestIdTokens(_keys.KeyId);
        SetupRefresh(Advertising("RS256", fresh.PublicKey));

        var result = await _validator.ValidateAsync(Authority, Advertising("RS256", _keys.PublicKey), ClientId, fresh.Sign(ValidClaims()), Nonce);

        result.Subject.Should().Be("user-sub");
        VerifyRefreshedOnce();
    }

    private void VerifyExpiredCertificateWarnings(Times times)
    {
        _loggerMock.Verify(x => x.Log(
            LogLevel.Warning,
            It.IsAny<EventId>(),
            It.Is<It.IsAnyType>((v, _) => v.ToString()!.Contains("signing certificate of " + Authority + " expired on 2026-05-08")),
            It.IsAny<Exception?>(),
            It.IsAny<Func<It.IsAnyType, Exception?, string>>()), times);
    }

    private X509SecurityKey ExpiredCertificateKey()
    {
        var certificate = _keys.Certificate(new DateTimeOffset(2025, 5, 8, 12, 0, 0, TimeSpan.Zero), new DateTimeOffset(2026, 5, 8, 12, 0, 0, TimeSpan.Zero));
        return new X509SecurityKey(certificate, _keys.KeyId);
    }

    [Fact]
    public async Task ValidateAsync_ShouldAcceptACorrectlySignedToken_WhenTheSigningCertificateHasExpired()
    {
        var configuration = Advertising("RS256", ExpiredCertificateKey());

        var first = await _validator.ValidateAsync(Authority, configuration, ClientId, _keys.Sign(ValidClaims()), Nonce);
        var second = await _validator.ValidateAsync(Authority, configuration, ClientId, _keys.Sign(ValidClaims()), Nonce);

        first.Subject.Should().Be("user-sub");
        second.Subject.Should().Be("user-sub");
        VerifyExpiredCertificateWarnings(Times.Once());
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldWarnAgain_AfterTheConfigurationWasRefreshed()
    {
        await _validator.ValidateAsync(Authority, Advertising("RS256", ExpiredCertificateKey()), ClientId, _keys.Sign(ValidClaims()), Nonce);
        await _validator.ValidateAsync(Authority, Advertising("RS256", ExpiredCertificateKey()), ClientId, _keys.Sign(ValidClaims()), Nonce);

        VerifyExpiredCertificateWarnings(Times.Exactly(2));
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldStillRejectAWrongSignature_WhenTheSigningCertificateHasExpired()
    {
        using var attacker = new TestIdTokens(_keys.KeyId);
        SetupRefresh(Advertising("RS256", ExpiredCertificateKey()));

        var act = () => _validator.ValidateAsync(Authority, Advertising("RS256", ExpiredCertificateKey()), ClientId, attacker.Sign(ValidClaims()), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyExpiredCertificateWarnings(Times.Never());
        VerifyRefreshedOnce();
    }

    [Fact]
    public async Task ValidateAsync_ShouldNotWarn_WhenTheSigningCertificateIsStillValid()
    {
        var certificate = _keys.Certificate(DateTimeOffset.UtcNow.AddDays(-1), DateTimeOffset.UtcNow.AddYears(1));

        var result = await _validator.ValidateAsync(Authority, Advertising("RS256", new X509SecurityKey(certificate, _keys.KeyId)), ClientId, _keys.Sign(ValidClaims()), Nonce);

        result.Subject.Should().Be("user-sub");
        _loggerMock.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(), It.IsAny<It.IsAnyType>(), It.IsAny<Exception?>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
        VerifyNoOtherCalls();
    }

    [Fact]
    public async Task ValidateAsync_ShouldNotRefresh_WhenTheTokenFailsForAnotherReason()
    {
        var claims = ValidClaims();
        claims["aud"] = "another-client";

        var act = () => _validator.ValidateAsync(Authority, Advertising("RS256", _keys.PublicKey), ClientId, _keys.Sign(claims), Nonce);

        await act.Should().ThrowAsync<DomainException>().WithMessage(Constants.Errors.OidcIdTokenInvalid);
        VerifyNoOtherCalls();
    }
}
