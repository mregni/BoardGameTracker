using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Extensions;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Host.Startup;
using Microsoft.AspNetCore.DataProtection;
using Serilog;

var logLevel = LogLevelExtensions.GetEnvironmentLogLevel();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .WriteTo.Console()
    .WriteTo.File(
        path: Path.Combine("logs", "app-.log"),
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        shared: true,
        flushToDiskInterval: TimeSpan.FromSeconds(1))
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreService();
builder.Services.AddHostedService<UpdateCheckBackgroundService>();

builder.WebHost.UseConfiguredSentry();
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());

builder.Host.UseSerilog();

builder.Services.AddHealthChecks()
    .AddDbContextCheck<BoardGameTracker.Core.Datastore.MainDbContext>();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddScoped<AuthDisabledFilter>();
builder.Services.AddSingleton<AuthSessionCookies>();
builder.Services.AddProblemDetails();
var environmentProvider = new EnvironmentProvider();
var startupProblem = StartupSecrets.FindProblem(
    environmentProvider.JwtSecret ?? builder.Configuration["Jwt:Secret"],
    environmentProvider.DataProtectionKey,
    environmentProvider.AuthEnabled);
if (startupProblem != null)
{
    Log.Fatal("BoardGameTracker cannot start: {Problem}", startupProblem);
    await Log.CloseAndFlushAsync();
    Environment.ExitCode = 1;
    return;
}

builder.Services.AddDataProtection()
    .PersistKeysToDbContext<MainDbContext>()
    .SetApplicationName("boardgametracker")
    .ProtectKeysWithSecret(environmentProvider.DataProtectionKey);

var trustedProxies = TrustedProxyList.Parse(environmentProvider.TrustedProxies);

builder.Services.AddTrustedProxies(trustedProxies);

builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
    httpClientBuilder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30)));

builder.Services.AddAppIdentity();

var authEnabled = environmentProvider.AuthEnabled;
var jwtSecret = StartupSecrets.ResolveJwtSecret(environmentProvider.JwtSecret ?? builder.Configuration["Jwt:Secret"], authEnabled);

builder.Services.AddAppJwtAuthentication(builder.Configuration, jwtSecret);
builder.Services.AddAppRateLimiting();
builder.Services.AddAppHttpClients();
builder.Services.AddMemoryCache();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddResponseCompression();
builder.Services.AddAppCors(environmentProvider);
builder.Services.AddAppControllers();

var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

builder.Services.AddAppSwagger(version);
builder.Services.AddExternalApiClients();

var app = builder.Build();
if (args is ["--openapi", var openApiPath])
{
    await StartupTasks.WriteOpenApiDocument(app.Services, openApiPath);
    await Log.CloseAndFlushAsync();
    return;
}

StartupTasks.CreateFolders(app.Services);

app.UseAppRequestLogging();

foreach (var entry in trustedProxies.Invalid)
{
    Log.Error("TRUSTED_PROXIES entry {Entry} is not an IP address or CIDR network and is ignored", entry);
}

if (trustedProxies.HasEntries)
{
    app.UseForwardedHeaders();
}
else
{
    app.UseMiddleware<UntrustedProxyWarningMiddleware>();
}

app.UseResponseCompression();

app.UseSecurityHeaders(!app.Environment.IsDevelopment(), environmentProvider.SwaggerEnabled);

app.UseExceptionHandler();

app.UseRouting();

app.UseCors("Allow");

app.UseAuthDisabledMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

app.MapHealthChecks("/api/health");

app.MapControllers();

if (environmentProvider.SwaggerEnabled)
{
    app.UseAppSwaggerUi();
}

if (bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var sentryEnabled) && sentryEnabled)
{
    app.UseSentryTracing();
}

app.UseImageFiles();

StartupTasks.LogStartupSummary(app.Services, environmentProvider, authEnabled);

if (!app.Environment.IsDevelopment())
{
    app.UseSpaFiles();
}

await StartupTasks.PrepareDataAsync(app.Services, authEnabled, environmentProvider.AdminPassword);

await app.RunAsync();

await Log.CloseAndFlushAsync();
