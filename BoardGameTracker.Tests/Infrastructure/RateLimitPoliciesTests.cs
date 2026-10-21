using System.Net;
using System.Reflection;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.RateLimiting;
using Xunit;

namespace BoardGameTracker.Tests.Infrastructure;

public class RateLimitPoliciesTests
{
    private static string? PolicyOf(Type controller, string action) =>
        controller.GetMethod(action)!.GetCustomAttribute<EnableRateLimitingAttribute>()?.PolicyName;

    [Theory]
    [InlineData(nameof(OidcController.Login))]
    [InlineData(nameof(OidcController.Callback))]
    [InlineData(nameof(OidcController.LinkCallback))]
    [InlineData(nameof(OidcController.Adopt))]
    public void SingleSignOnSteps_ShouldUseTheirOwnLimiter(string action)
    {
        PolicyOf(typeof(OidcController), action).Should().Be(RateLimitPolicies.Oidc);
    }

    [Theory]
    [InlineData(nameof(AuthController.Login))]
    [InlineData(nameof(AuthController.ChangePassword))]
    [InlineData(nameof(AuthController.ForgotPassword))]
    [InlineData(nameof(AuthController.ResetPasswordWithToken))]
    public void PasswordEndpoints_ShouldKeepTheStrictLimiter(string action)
    {
        PolicyOf(typeof(AuthController), action).Should().Be(RateLimitPolicies.Auth);
    }

    [Fact]
    public void OidcLimiter_ShouldAllowSeveralSignInsPerMinuteBehindOneAddress()
    {
        const int requestsPerSignIn = 3;
        (RateLimitPolicies.OidcPermitsPerMinute / requestsPerSignIn).Should().BeGreaterThanOrEqualTo(15);
        RateLimitPolicies.AuthPermitsPerMinute.Should().Be(10);
    }

    [Fact]
    public async Task Partitions_ShouldLimitPerClientAddress_WithTheirOwnBudget()
    {
        var context = new DefaultHttpContext();
        context.Connection.RemoteIpAddress = IPAddress.Parse("203.0.113.9");

        (await DrainAsync(RateLimitPolicies.AuthPartition(context))).Should().Be(RateLimitPolicies.AuthPermitsPerMinute);
        (await DrainAsync(RateLimitPolicies.OidcPartition(context))).Should().Be(RateLimitPolicies.OidcPermitsPerMinute);
        RateLimitPolicies.AuthPartition(context).PartitionKey.Should().Be(RateLimitPolicies.OidcPartition(context).PartitionKey);
    }

    private static async Task<int> DrainAsync(System.Threading.RateLimiting.RateLimitPartition<string> partition)
    {
        using var limiter = partition.Factory(partition.PartitionKey);
        var granted = 0;
        while (granted < 1000)
        {
            using var lease = await limiter.AcquireAsync(1, TestContext.Current.CancellationToken);
            if (!lease.IsAcquired)
            {
                break;
            }

            granted++;
        }

        return granted;
    }
}
