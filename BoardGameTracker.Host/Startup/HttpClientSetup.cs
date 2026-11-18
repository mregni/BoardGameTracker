using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Auth.Pictures;
using BoardGameTracker.Core.ChangeDetection;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Images;
using BoardGameTracker.Core.Integrations;
using BoardGameTracker.Core.Rag;
using BoardGameTracker.Core.Settings.Interfaces;
using Refit;

namespace BoardGameTracker.Host.Startup;

internal static class HttpClientSetup
{
    public static IServiceCollection AddAppHttpClients(this IServiceCollection services)
    {
        services.AddHttpClient();
        services.AddHttpClient(AiClientFactory.HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5));
        services.AddHttpClient(ChangeDetectionClient.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                client.Timeout = TimeSpan.FromSeconds(10);
                client.MaxResponseContentBufferSize = 1024 * 1024;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHttpClient(BggConnectionTester.HttpClientName)
            .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(10));
        services.AddHttpClient(OidcService.HttpClientName)
            .ConfigureHttpClient(client => client.MaxResponseContentBufferSize = 1024 * 1024)
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
        services.AddHttpClient(IdentityProviderPictureDownloader.HttpClientName)
            .ConfigureHttpClient(client =>
            {
                client.Timeout = IdentityProviderPictureDownloader.Timeout;
                client.MaxResponseContentBufferSize = IdentityProviderPictureDownloader.MaxBytes;
            })
            .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler
            {
                AllowAutoRedirect = false,
                UseProxy = false,
                ConnectCallback = PinnedAddressConnector.ConnectAsync,
            });
        services.AddHttpClient(ImageService.HttpClientName)
            .AddStandardResilienceHandler();
        return services;
    }

    public static IServiceCollection AddExternalApiClients(this IServiceCollection services)
    {
        services.AddHttpClient(nameof(IBoardGameGeekXmlApi2Client));
        services.AddScoped<IBoardGameGeekXmlApi2Client>(sp =>
        {
            var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
            var settingsService = sp.GetRequiredService<ISettingsService>();
            return new LazyBoardGameGeekClient(
                () => httpClientFactory.CreateClient(nameof(IBoardGameGeekXmlApi2Client)),
                settingsService);
        });

        services.AddRefitClient<IDockerHubApi>()
            .ConfigureHttpClient(options =>
            {
                options.BaseAddress = new Uri("https://hub.docker.com");
            })
            .AddStandardResilienceHandler();
        return services;
    }
}
