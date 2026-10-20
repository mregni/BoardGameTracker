using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Core.Integrations;
using BoardGameTracker.Core.Maintenance.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
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
    public static readonly string DataProtectionKey = Convert.ToBase64String(RandomNumberGenerator.GetBytes(48));

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
    private readonly List<WebApplicationFactory<Program>> _variants = [];
    private readonly Dictionary<string, string> _tokens = new();
    private readonly Dictionary<string, string?> _originalEnvironmentVariables = new();

    private int _clients;

    public HttpClient CreateClient() => WithClientAddress(_factory.CreateClient());

    public HttpClient CreateBrowserClient(Uri? baseAddress = null) => WithClientAddress(_factory.CreateClient(new WebApplicationFactoryClientOptions
    {
        AllowAutoRedirect = false,
        HandleCookies = true,
        BaseAddress = baseAddress ?? new Uri("http://localhost"),
    }));

    private HttpClient WithClientAddress(HttpClient client)
    {
        var id = Interlocked.Increment(ref _clients);
        client.DefaultRequestHeaders.Add(TestClientAddressStartupFilter.HeaderName, $"10.{id / 65536 % 256}.{id / 256 % 256}.{id % 256}");
        return client;
    }

    public IServiceProvider Services => _factory.Services;

    public HttpClient CreateClientWith(Action<IServiceCollection> configureServices)
    {
        var variant = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configureServices));
        _variants.Add(variant);
        return WithClientAddress(variant.CreateClient());
    }

    public HttpClient CreateBrowserClientWith(Action<IServiceCollection> configureServices, Uri baseAddress)
    {
        var variant = _factory.WithWebHostBuilder(builder => builder.ConfigureTestServices(configureServices));
        _variants.Add(variant);
        return WithClientAddress(variant.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            HandleCookies = true,
            BaseAddress = baseAddress,
        }));
    }

    public async ValueTask InitializeAsync()
    {
        await _postgres.StartAsync();

        WriteSpaStub();
        SetEnvironmentVariable("DB_HOST", _postgres.Hostname);
        SetEnvironmentVariable("DB_PORT", _postgres.GetMappedPublicPort(5432).ToString());
        SetEnvironmentVariable("DB_USER", "bgt");
        SetEnvironmentVariable("DB_PASSWORD", "bgt");
        SetEnvironmentVariable("DB_NAME", "boardgametracker");
        SetEnvironmentVariable("JWT_SECRET", "integration-test-secret-that-is-long-enough-for-the-guard");
        SetEnvironmentVariable("DATA_PROTECTION_KEY", DataProtectionKey);
        SetEnvironmentVariable("ADMIN_PASSWORD", AdminPassword);
        SetEnvironmentVariable("AUTH_ENABLED", "true");
        SetEnvironmentVariable("RAG_ENABLED", "false");
        SetEnvironmentVariable("UPDATE_CHECK_ENABLED", "false");
        SetEnvironmentVariable("STATISTICS_ENABLED", "false");
        SetEnvironmentVariable("LOGLEVEL", "warn");
        SetEnvironmentVariable("ASPNETCORE_ENVIRONMENT", "Production");

        _factory = new WebApplicationFactory<Program>().WithWebHostBuilder(builder =>
        {
            builder.UseContentRoot(Directory.GetCurrentDirectory());
            builder.ConfigureServices(services =>
            {
                services.AddTransient<IStartupFilter, TestClientAddressStartupFilter>();
                services.AddHttpClient(BggConnectionTester.HttpClientName)
                    .ConfigurePrimaryHttpMessageHandler(() => new FakeBggHandler());
            });
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

    private void SetEnvironmentVariable(string name, string value)
    {
        _originalEnvironmentVariables.TryAdd(name, Environment.GetEnvironmentVariable(name));
        Environment.SetEnvironmentVariable(name, value);
    }

    public async ValueTask DisposeAsync()
    {
        foreach (var variant in _variants)
        {
            await variant.DisposeAsync();
        }

        await _factory.DisposeAsync();
        await _postgres.DisposeAsync();
        foreach (var kvp in _originalEnvironmentVariables)
        {
            Environment.SetEnvironmentVariable(kvp.Key, kvp.Value);
        }
    }

    public async Task<HttpClient> CreateClientAsAsync(string username)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", await GetTokenAsync(username));
        return client;
    }

    public Task<HttpClient> CreateAdminClientAsync() => CreateClientAsAsync(AdminUsername);

    public AsyncServiceScope CreateScope() => _factory.Services.CreateAsyncScope();

    public string GetConnectionString(string database) =>
        new NpgsqlConnectionStringBuilder(_postgres.GetConnectionString()) { Database = database }.ConnectionString;

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
