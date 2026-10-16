using System.Net;

namespace BoardGameTracker.IntegrationTests.Infrastructure;

public sealed class FakeBggHandler : HttpMessageHandler
{
    public const string ValidKey = "integration-valid-bgg-key";

    protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var authorized = request.Headers.Authorization is { Scheme: "Bearer", Parameter: ValidKey };
        return Task.FromResult(new HttpResponseMessage(authorized ? HttpStatusCode.OK : HttpStatusCode.Unauthorized));
    }
}
