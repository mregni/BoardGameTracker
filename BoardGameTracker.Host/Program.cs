using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using Ardalis.GuardClauses;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Bgg.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Http;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Refit;
using Serilog;

var logLevel = LogLevelExtensions.GetEnvironmentLogLevel();

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Is(logLevel)
    .MinimumLevel.Override("Microsoft.AspNetCore", Serilog.Events.LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", Serilog.Events.LogEventLevel.Error)
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

builder.Services.AddHealthChecks();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddProblemDetails();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

builder.Services.Configure<HttpClientFactoryOptions>(options =>
{
    options.HttpClientActions.Add(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(30);
    });
});

builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 4;
        options.User.RequireUniqueEmail = false;
    })
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders();

var jwtSecret = Environment.GetEnvironmentVariable("JWT_SECRET") ?? builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "boardgametracker-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "boardgametracker-client";
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    throw new ArgumentException("JWT_SECRET not set");
}

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddHttpClient();
builder.Services.AddMemoryCache();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddResponseCompression();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Allow",
        policyBuilder =>
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader());
});

builder.Services
    .AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
        options.Filters.Add<ValidateIdFilter>();
    })
    .AddJsonOptions(options =>
    {
        ApplySerializerSettings(options.JsonSerializerOptions);
    })
    .AddControllersAsServices();

var version = typeof(Program).Assembly.GetName().Version?.ToString(3) ?? "0.0.0";

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "BoardGameTracker API",
        Version = version,
        Description = "BoardGameTracker API for managing board game collections and play sessions"
    });
});

var refitSettings = new RefitSettings
{
    ContentSerializer = new XmlContentSerializer()
};
builder.Services.AddRefitClient<IBggApi>(refitSettings)
    .ConfigureHttpClient(options =>
    {
        options.BaseAddress = new Uri("https://boardgamegeek.com/xmlapi2");
    });

builder.Services.AddRefitClient<IDockerHubApi>()
    .ConfigureHttpClient(options =>
    {
        options.BaseAddress = new Uri("https://hub.docker.com");
    });

builder.Services.AddSpaStaticFiles(configuration => {
    configuration.RootPath = "wwwroot";
});

var app = builder.Build();
CreateFolders(app.Services);

app.UseSerilogRequestLogging();

app.UseForwardedHeaders();

app.UseRouting();

app.UseCors("Allow");

app.UseAuthBypassIfEnabled();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health");

app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

if (bool.TryParse(Environment.GetEnvironmentVariable("STATISTICS_ENABLED"), out var sentryEnabled) && sentryEnabled)
{
    app.UseSentryTracing();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullCoverImagePath),
    RequestPath = "/images/cover"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullCommonImagePath),
    RequestPath = "/images/common"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullBadgeImagePath),
    RequestPath = "/images/badges"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullProfileImagePath),
    RequestPath = "/images/profile"
});

var logger = app.Services.GetService<ILogger<Program>>();

logger.LogInformation("BoardGameTracker started");
logger.LogInformation("  Environment:  {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
logger.LogInformation("  Log level:    {LogLevel}", LogLevelExtensions.GetEnvironmentLogLevel());
logger.LogInformation("  Sentry:       {SentryEnabled}", Environment.GetEnvironmentVariable("STATISTICS_ENABLED")?.ToLower() == "true" ? "Enabled" : "Disabled");
logger.LogInformation("  HTTP ports:   {HttpPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "default");
logger.LogInformation("  HTTPS ports:  {HttpsPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS") ?? "not configured");
logger.LogInformation("  Timezone:     {Timezone}", Environment.GetEnvironmentVariable("TZ") ?? "system default");
logger.LogInformation("  DB port:      {DbPort}", Environment.GetEnvironmentVariable("DB_PORT") ?? "5432");
logger.LogInformation("  Auth bypass:  {AuthBypass}", Environment.GetEnvironmentVariable("AUTH_BYPASS")?.ToLower() == "true" ? "Yes" : "No");

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseSpaStaticFiles();
    app.UseStaticFiles();
    app.UseSpa(config => {
        config.Options.SourcePath = "wwwroot";
        config.Options.DefaultPageStaticFileOptions = new StaticFileOptions
        {
            OnPrepareResponse = ctx =>
            {
                var headers = ctx.Context.Response.GetTypedHeaders();
                headers.CacheControl = new CacheControlHeaderValue
                {
                    NoCache = true,
                    NoStore = true,
                    MustRevalidate = true
                };
            }
        };
    });
}

RunDbMigrations(app.Services);
await SeedConfig(app.Services);
await SeedAuthData(app.Services);

await app.RunAsync();

await Log.CloseAndFlushAsync();

static void RunDbMigrations(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = Guard.Against.Null(scope.ServiceProvider.GetRequiredService<MainDbContext>());
    context.Database.Migrate();
}

static void CreateFolders(IServiceProvider serviceProvider)
{
    var diskProvider = Guard.Against.Null(serviceProvider.GetService<IDiskProvider>());

    diskProvider.EnsureFolder(PathHelper.FullRootImagePath);
    diskProvider.EnsureFolder(PathHelper.FullCoverImagePath);
    diskProvider.EnsureFolder(PathHelper.FullProfileImagePath);
    diskProvider.EnsureFolder(PathHelper.FullBadgeImagePath);
    diskProvider.EnsureFolder(PathHelper.FullCommonImagePath);
}

static async Task SeedConfig(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var configRepository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
    await configRepository.SeedConfigAsync(ConfigDefaults.All);
}

static async Task SeedAuthData(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAuthData(roleManager, userManager, seedLogger);
}

static void ApplySerializerSettings(JsonSerializerOptions serializerSettings)
{
    serializerSettings.AllowTrailingCommas = true;
    serializerSettings.DefaultIgnoreCondition = JsonIgnoreCondition.Never;
    serializerSettings.PropertyNameCaseInsensitive = true;
    serializerSettings.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    serializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    serializerSettings.WriteIndented = true;

    // Ensure all DateTime values are handled as UTC
    serializerSettings.Converters.Add(new UtcDateTimeConverter());
    serializerSettings.Converters.Add(new UtcNullableDateTimeConverter());
    serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}
