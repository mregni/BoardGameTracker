using System.Net;
using System.Net.Http.Json;
using System.Text.RegularExpressions;
using BoardGameTracker.Common;
using BoardGameTracker.IntegrationTests.Infrastructure;
using FluentAssertions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Api;

[Collection(IntegrationCollection.Name)]
public partial class EndpointAuthorizationTests
{
    private readonly IntegrationFixture _fixture;

    public EndpointAuthorizationTests(IntegrationFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public void EveryAction_ShouldDeclareWhoMayCallIt()
    {
        ApiEndpoints().Should().HaveCountGreaterThan(90);

        var undeclared = ApiEndpoints()
            .Where(e => !e.Endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>().Any() &&
                        e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() == null)
            .Select(e => e.Name)
            .ToList();

        undeclared.Should().BeEmpty();
    }

    [Fact]
    public async Task EveryProtectedAction_ShouldEnforceItsRoles()
    {
        var failures = new List<string>();
        var checkedEndpoints = 0;

        foreach (var endpoint in ApiEndpoints().Where(e => e.Endpoint.Metadata.GetMetadata<IAllowAnonymous>() == null))
        {
            var roleSets = endpoint.Endpoint.Metadata.GetOrderedMetadata<IAuthorizeData>()
                .Where(a => !string.IsNullOrWhiteSpace(a.Roles))
                .Select(a => a.Roles!.Split(',', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries))
                .ToList();

            checkedEndpoints++;
            using (var anonymous = _fixture.CreateClient())
            {
                var status = await SendAsync(anonymous, endpoint);
                if (status != HttpStatusCode.Unauthorized)
                {
                    failures.Add($"{endpoint.Name} as anonymous: {(int)status}");
                }
            }

            foreach (var role in new[] { Constants.AuthRoles.Reader, Constants.AuthRoles.User })
            {
                var allowed = roleSets.All(set => set.Contains(role));
                if (allowed && endpoint.Method != HttpMethods.Get)
                {
                    continue;
                }

                using var client = await _fixture.CreateClientAsAsync(role.ToLowerInvariant());
                var status = await SendAsync(client, endpoint);
                if (!allowed && status != HttpStatusCode.Forbidden)
                {
                    failures.Add($"{endpoint.Name} as {role}: expected 403, got {(int)status}");
                }
                else if (allowed && status is HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden)
                {
                    failures.Add($"{endpoint.Name} as {role}: expected access, got {(int)status}");
                }
            }
        }

        checkedEndpoints.Should().BeGreaterThan(70);
        failures.Should().BeEmpty();
    }

    private static async Task<HttpStatusCode> SendAsync(HttpClient client, ApiEndpoint endpoint)
    {
        using var request = new HttpRequestMessage(new HttpMethod(endpoint.Method), endpoint.Url);
        if (endpoint.Method is "POST" or "PUT" or "PATCH")
        {
            request.Content = JsonContent.Create(new { });
        }

        using var response = await client.SendAsync(request);
        return response.StatusCode;
    }

    private IEnumerable<ApiEndpoint> ApiEndpoints() =>
        _fixture.Services.GetRequiredService<EndpointDataSource>().Endpoints
            .OfType<RouteEndpoint>()
            .Where(e => e.Metadata.GetMetadata<ControllerActionDescriptor>() != null)
            .SelectMany(e => (e.Metadata.GetMetadata<HttpMethodMetadata>()?.HttpMethods ?? [HttpMethods.Get])
                .Select(method => new ApiEndpoint(e, method, "/" + RouteParameter().Replace(e.RoutePattern.RawText!.TrimStart('/'), SampleValue))))
            .OrderBy(e => e.Url)
            .ThenBy(e => e.Method);

    private static string SampleValue(Match match) =>
        match.Groups["constraint"].Value.Contains("guid", StringComparison.OrdinalIgnoreCase) ? Guid.NewGuid().ToString() : "1";

    [GeneratedRegex(@"\{\*{0,2}(?<name>[^}:?=]+)(:(?<constraint>[^}?=]+))?[?]?(=[^}]*)?\}")]
    private static partial Regex RouteParameter();

    private sealed record ApiEndpoint(RouteEndpoint Endpoint, string Method, string Url)
    {
        public string Name => $"{Method} {Endpoint.RoutePattern.RawText}";
    }
}
