using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using Testcontainers.PostgreSql;
using Xunit;

namespace BoardGameTracker.IntegrationTests.Infrastructure;

public sealed class IntegrationFixture : IAsyncLifetime
{
    public const string AdminUsername = "admin";
    public const string AdminPassword = "IntegrationTest123!";
    public const string UserPassword = "UserTest123!";
    public const string IndexMarker = "<!-- integration-index -->";
    public const string AssetMarker = "/* integration-asset */";

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) },
    };

    private readonly PostgreSqlContainer _postgres = new PostgreSqlBuilder("pgvector/pgvector:pg16")
        .WithDatabase("boardgametracker")
        .WithUsername("bgt")
        .WithPassword("bgt")
        .Build();

    private WebApplicationFactory<Program> _factory = null!;
    private readonly Dictionary<string, string> _tokens = new();

    private int _clients;

    public HttpClient CreateClient() => WithClientAddress(_factory.CreateClient());

    public HttpClient CreateBrowserClient() => WithClientAddress(_factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
    }));

    private HttpClient WithClientAddress(HttpClient client)
    {
        var id = Interlocked.Increment(ref _clients);
        client.DefaultRequestHeaders.Add(TestClientAddressStartupFilter.HeaderName, $"10.{id / 65536 % 256}.{id / 256 % 256}.{id % 256}");
        return client;
    }

    public IServiceProvider Services => _factory.Services;

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        WriteSpaStub();
        Environment.SetEnvironmentVariable("DB_HOST", _postgres.Hostname);
        Environment.SetEnvironmentVariable("DB_PORT", _postgres.GetMappedPublicPort(5432).ToString());
        Environment.SetEnvironmentVariable("DB_USER", "bgt");
        Environment.SetEnvironmentVariable("DB_PASSWORD", "bgt");
        Environment.SetEnvironmentVariable("DB_NAME", "boardgametracker");
        Environment.SetEnvironmentVariable("JWT_SECRET", "integration-test-secret-that-is-long-enough-for-the-guard");
        Environment.SetEnvironmentVariable("ADMIN_PASSWORD", AdminPassword);
        Environment.SetEnvironmentVariable("AUTH_ENABLED", "true");
        Environment.SetEnvironmentVariable("RAG_ENABLED", "false");
        Environment.SetEnvironmentVariable("UPDATE_CHECK_ENABLED", "false");
        Environment.SetEnvironmentVariable("STATISTICS_ENABLED", "false");
        Environment.SetEnvironmentVariable("LOGLEVEL", "warn");
        Environment.SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureServices(services => services.AddTransient<IStartupFilter, TestClientAddressStartupFilter>());
        });
        using var client = _factory.CreateClient();
        var status = await client.GetAsync("/api/health");
        await EnsureSuccessAsync(status, "health check");

        await RegisterAsync("reader", Constants.AuthRoles.Reader);
        await RegisterAsync("user", Constants.AuthRoles.User);
        await GetTokenAsync("reader");
        await GetTokenAsync("user");
    }

    private static void WriteSpaStub()
    {
        var root = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
        Directory.CreateDirectory(Path.Combine(root, "assets"));
        File.WriteAllText(Path.Combine(root, "index.html"), $"<!doctype html><html><body>{IndexMarker}</body></html>");
        File.WriteAllText(Path.Combine(root, "assets", "app.js"), AssetMarker);
    }

    public async ValueTask DisposeAsync()
    {
        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
    }

    public async Task<HttpClient> CreateClientAsAsync(string username)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(username));
        return client;
    }

    public Task<HttpClient> CreateAdminClientAsync() => CreateClientAsAsync(AdminUsername);

    public AsyncServiceScope CreateScope() => _factory.Services.CreateAsyncScope();

    public async Task RestoreTestUsersAsync()
    {
        _tokens.Clear();
        await RegisterAsync("reader", Constants.AuthRoles.Reader);
        await RegisterAsync("user", Constants.AuthRoles.User);
    }

    public async Task ResetDataAsync()
    {
        await using var scope = CreateScope();
        await scope.ServiceProvider.GetRequiredService<IMaintenanceRepository>().ClearUserDataAsync();
    }

    private async Task<string> GetTokenAsync(string username)
    {
        if (_tokens.TryGetValue(username, out var cached))
        {
            return cached;
        }

        using var client = CreateClient();
        var password = username == AdminUsername ? AdminPassword : UserPassword;
        var response = await client.PostAsJsonAsync("/api/auth/login", new LoginRequest(username, password));
        await EnsureSuccessAsync(response, $"login as {username}");
        var login = await response.Content.ReadFromJsonAsync<LoginResponse>();
        _tokens[username] = login!.AccessToken;
        return login.AccessToken;
    }

    private async Task RegisterAsync(string username, string role)
    {
        using var admin = await CreateAdminClientAsync();
        var response = await admin.PostAsJsonAsync("/api/auth/register", new RegisterRequest(username, $"{username}@example.com", UserPassword, role));
        await EnsureSuccessAsync(response, $"register {username}");
    }

    private static async Task EnsureSuccessAsync(HttpResponseMessage response, string step)
    {
        if (!response.IsSuccessStatusCode)
        {
            throw new InvalidOperationException($"{step} failed with {(int)response.StatusCode}: {await response.Content.ReadAsStringAsync()}");
        }
    }
}

[CollectionDefinition(Name)]
public sealed class IntegrationCollection : ICollectionFixture<IntegrationFixture>
{
    public const string Name = "Integration";
}
