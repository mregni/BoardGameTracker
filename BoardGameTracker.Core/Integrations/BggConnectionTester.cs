using System.Net;
using System.Net.Http.Headers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Integrations;
using BoardGameTracker.Core.Integrations.Interfaces;
using BoardGameTracker.Core.Settings.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Integrations;

public class BggConnectionTester : IIntegrationTester
{
    public const string HttpClientName = "bgg-status";
    public const string ProbeUrl = "https://boardgamegeek.com/xmlapi2/thing?id=13";

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISettingsService _settingsService;
    private readonly ILogger<BggConnectionTester> _logger;

    public BggConnectionTester(
        IHttpClientFactory httpClientFactory,
        ISettingsService settingsService,
        ILogger<BggConnectionTester> logger)
    {
        _httpClientFactory = httpClientFactory;
        _settingsService = settingsService;
        _logger = logger;
    }

    public IntegrationKind Integration => IntegrationKind.Bgg;

    public async Task<IntegrationTestResult> TestAsync(CancellationToken cancellationToken = default)
    {
        var apiKey = await _settingsService.GetBggApiKeyAsync();
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return IntegrationTestResult.NotConfigured;
        }

        try
        {
            var client = _httpClientFactory.CreateClient(HttpClientName);
            using var request = new HttpRequestMessage(HttpMethod.Get, ProbeUrl);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", apiKey.Trim());
            using var response = await client.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            if (!response.IsSuccessStatusCode)
            {
                _logger.LogWarning("BoardGameGeek connection test returned {StatusCode}", (int)response.StatusCode);
            }

            return Map(response.StatusCode);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogWarning("BoardGameGeek connection test timed out");
            return IntegrationTestResult.Unreachable(IntegrationMessageKeys.Timeout);
        }
        catch (HttpRequestException ex)
        {
            _logger.LogWarning("BoardGameGeek connection test failed: {Error}", ex.HttpRequestError);
            return IntegrationTestResult.Unreachable();
        }
        catch (FormatException)
        {
            _logger.LogWarning("BoardGameGeek API key cannot be sent as a bearer token");
            return IntegrationTestResult.Unauthorized;
        }
    }

    private static IntegrationTestResult Map(HttpStatusCode statusCode) => statusCode switch
    {
        _ when (int)statusCode is >= 200 and < 300 => IntegrationTestResult.Ok,
        HttpStatusCode.Unauthorized or HttpStatusCode.Forbidden => IntegrationTestResult.Unauthorized,
        HttpStatusCode.TooManyRequests => IntegrationTestResult.RateLimited,
        _ => IntegrationTestResult.Unreachable(IntegrationMessageKeys.UnexpectedResponse)
    };
}
