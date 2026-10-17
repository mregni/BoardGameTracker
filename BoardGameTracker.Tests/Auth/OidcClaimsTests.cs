using BoardGameTracker.Core.Auth;
using FluentAssertions;
using Xunit;

namespace BoardGameTracker.Tests.Auth;

public class OidcClaimsTests
{
    [Fact]
    public void GetValue_ShouldReadStringsNumbersAndArrays()
    {
        var claims = OidcClaims.Parse("""{"sub":"s-1","age":42,"verified":true,"groups":["a","b"],"none":null}""");

        claims.Subject.Should().Be("s-1");
        claims.GetValue("age").Should().Be("42");
        claims.GetValue("verified").Should().Be("true");
        claims.GetValue("groups").Should().Be("a,b");
        claims.GetValue("none").Should().BeNull();
        claims.GetValue("missing").Should().BeNull();
    }

    [Fact]
    public void GetValues_ShouldSplitScalarsAndSkipEmptyArrayEntries()
    {
        var claims = OidcClaims.Parse("""{"scalar":" a, b ,,c ","array":[" x ","",null,"y"],"empty":""}""");

        claims.GetValues("scalar").Should().Equal("a", "b", "c");
        claims.GetValues("array").Should().Equal("x", "y");
        claims.GetValues("empty").Should().BeEmpty();
        claims.GetValues("missing").Should().BeEmpty();
    }

    [Fact]
    public void MergeWith_ShouldPreferTheOtherClaims_ButNeverItsSubject()
    {
        var idToken = OidcClaims.Parse("""{"sub":"from-id-token","name":"Old","email":"a@example.com"}""");
        var userInfo = OidcClaims.Parse("""{"sub":"from-userinfo","name":"New"}""");

        var merged = idToken.MergeWith(userInfo);

        merged.Subject.Should().Be("from-id-token");
        merged.GetValue("name").Should().Be("New");
        merged.GetValue("email").Should().Be("a@example.com");
    }

    [Fact]
    public void Parse_ShouldTreatANonObjectAsEmpty()
    {
        OidcClaims.Parse("[1,2]").Subject.Should().BeNull();
    }

    [Theory]
    [InlineData("https://sso.example.com/realms/x/", "https://SSO.example.com/realms/x", true)]
    [InlineData(" https://sso.example.com ", "https://sso.example.com/", true)]
    [InlineData("https://sso.example.com/realms/x", "https://sso.example.com/realms/y", false)]
    public void OidcIssuer_ShouldCompareIgnoringCaseAndTrailingSlashes(string issuer, string authority, bool expected)
    {
        OidcIssuer.Matches(issuer, authority).Should().Be(expected);
        OidcIssuer.Normalize(issuer).Should().NotEndWith("/");
    }
}
