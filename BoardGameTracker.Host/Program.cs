using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Ardalis.GuardClauses;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Auth;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Updates;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Extensions;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
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
builder.Services.AddScoped<AuthDisabledFilter>();
builder.Services.AddProblemDetails();

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    var trustedProxies = new EnvironmentProvider().TrustedProxies;
    foreach (var proxy in trustedProxies)
    {
        if (IPAddress.TryParse(proxy, out var address))
        {
            options.KnownProxies.Add(address);
        }
        else if (System.Net.IPNetwork.TryParse(proxy, out var network))
        {
            options.KnownIPNetworks.Add(network);
        }
    }
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
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = false;
        options.Lockout.AllowedForNewUsers = true;
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
    })
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders();

var environmentProvider = new EnvironmentProvider();
var authEnabled = environmentProvider.AuthEnabled;
var jwtSecret = environmentProvider.JwtSecret ?? builder.Configuration["Jwt:Secret"];
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "boardgametracker-api";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "boardgametracker-client";
if (string.IsNullOrWhiteSpace(jwtSecret))
{
    if (authEnabled)
    {
        throw new ArgumentException("JWT_SECRET not set");
    }

    jwtSecret = "auth-disabled-placeholder-key-not-used";
}
else if (authEnabled && jwtSecret.Length < 32)
{
    throw new ArgumentException(
        $"JWT_SECRET must be at least 32 characters long, but was {jwtSecret.Length}.");
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
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        context.Connection.RemoteIpAddress?.ToString() ?? "unknown",
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(BoardGameTracker.Core.Rag.AiClientFactory.HttpClientName)
    .ConfigureHttpClient(client => client.Timeout = System.Threading.Timeout.InfiniteTimeSpan);
builder.Services.AddHttpClient(BoardGameTracker.Core.ChangeDetection.ChangeDetectionClient.HttpClientName);
builder.Services.AddMemoryCache();

builder.Services.AddRouting(options => options.LowercaseUrls = true);
builder.Services.AddResponseCompression();
var corsOrigins = environmentProvider.CorsOrigins;
builder.Services.AddCors(options =>
{
    options.AddPolicy("Allow", policyBuilder =>
    {
        if (corsOrigins.Count > 0)
        {
            policyBuilder
                .WithOrigins([.. corsOrigins])
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
        else if (environmentProvider.IsDevelopment)
        {
            policyBuilder
                .AllowAnyOrigin()
                .AllowAnyMethod()
                .AllowAnyHeader();
        }
    });
});

var mvcBuilder = builder.Services
    .AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
        options.Filters.Add<ValidateIdFilter>();
    })
    .AddJsonOptions(options =>
    {
        ApplySerializerSettings(options.JsonSerializerOptions);
    });

var apiAssembly = typeof(GlobalExceptionHandler).Assembly;
if (mvcBuilder.PartManager.ApplicationParts.OfType<AssemblyPart>().All(part => part.Assembly != apiAssembly))
{
    mvcBuilder.AddApplicationPart(apiAssembly);
}

mvcBuilder.AddControllersAsServices();

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

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Description = "Call POST /api/auth/login and the token is captured automatically, or paste a JWT here.",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT"
    });

    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        { new OpenApiSecuritySchemeReference("Bearer", document), new List<string>() }
    });
});

builder.Services.AddHttpClient(nameof(IBoardGameGeekXmlApi2Client));
builder.Services.AddScoped<IBoardGameGeekXmlApi2Client>(sp =>
{
    var httpClientFactory = sp.GetRequiredService<IHttpClientFactory>();
    var settingsService = sp.GetRequiredService<ISettingsService>();
    return new LazyBoardGameGeekClient(
        () => httpClientFactory.CreateClient(nameof(IBoardGameGeekXmlApi2Client)),
        settingsService);
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

var hstsEnabled = !app.Environment.IsDevelopment();
var swaggerEnabled = environmentProvider.SwaggerEnabled;
app.Use(async (context, next) =>
{
    context.Response.OnStarting(() =>
    {
        var headers = context.Response.Headers;
        headers["X-Content-Type-Options"] = "nosniff";
        headers["X-Frame-Options"] = "DENY";
        headers["Referrer-Policy"] = "strict-origin-when-cross-origin";
        headers["Cross-Origin-Resource-Policy"] = "same-origin";
        headers["Cross-Origin-Opener-Policy"] = "same-origin";
        headers["Cross-Origin-Embedder-Policy"] = "require-corp";
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        var isSwagger = swaggerEnabled && context.Request.Path.StartsWithSegments("/swagger");
        headers["Content-Security-Policy"] = isSwagger
            ? "default-src 'self'; img-src 'self' data: blob:; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; form-action 'self';"
            : "default-src 'self'; img-src 'self' data: blob:; script-src 'self'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; form-action 'self';";

        if (hstsEnabled && context.Request.IsHttps)
        {
            headers["Strict-Transport-Security"] = "max-age=2592000";
        }

        return Task.CompletedTask;
    });

    await next();
});

app.UseExceptionHandler();

app.UseRouting();

app.UseCors("Allow");

app.UseRateLimiter();
app.UseAuthDisabledMiddleware();
app.UseAuthentication();
app.UseAuthorization();

app.MapHealthChecks("/api/health");

app.MapControllers();

if (environmentProvider.SwaggerEnabled)
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.UseResponseInterceptor(
            "(res) => { try { if (res.status >= 200 && res.status < 300) { var body = res.obj || (res.text ? JSON.parse(res.text) : null); if (body && body.accessToken && window.ui) { window.ui.preauthorizeApiKey('Bearer', body.accessToken); console.log('[Swagger] Bearer token captured from auth response.'); } } } catch (e) { console.warn('[Swagger] auth interceptor failed', e); } return res; }");
    });
}

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
    FileProvider = new PhysicalFileProvider(PathHelper.FullProfileImagePath),
    RequestPath = "/images/profile"
});

var logger = app.Services.GetRequiredService<ILogger<Program>>();

logger.LogInformation("BoardGameTracker started");
logger.LogInformation("  Environment:  {Environment}", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
logger.LogInformation("  Log level:    {LogLevel}", LogLevelExtensions.GetEnvironmentLogLevel());
logger.LogInformation("  Sentry:       {SentryEnabled}", Environment.GetEnvironmentVariable("STATISTICS_ENABLED")?.ToLower() == "true" ? "Enabled" : "Disabled");
logger.LogInformation("  HTTP ports:   {HttpPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS") ?? "default");
logger.LogInformation("  Timezone:     {Timezone}", Environment.GetEnvironmentVariable("TZ") ?? "system default");
logger.LogInformation("  DB port:      {DbPort}", Environment.GetEnvironmentVariable("DB_PORT") ?? "5432");
logger.LogInformation("  Auth:         {AuthState}", authEnabled ? "Enabled" : "Disabled");

if (!app.Environment.IsDevelopment())
{
    app.UseSpaStaticFiles(new StaticFileOptions { OnPrepareResponse = SetUnhashedAssetCacheHeaders });
    app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetUnhashedAssetCacheHeaders });
    app.UseWhen(
        context => HttpMethods.IsGet(context.Request.Method) || HttpMethods.IsHead(context.Request.Method),
        spaBranch => spaBranch.UseSpa(config => {
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
    }));
}

RunDbMigrations(app.Services);
await SeedConfig(app.Services);
if (authEnabled)
{
    await SeedAuthData(app.Services, environmentProvider.AdminPassword);
}

await app.RunAsync();

await Log.CloseAndFlushAsync();

static void SetUnhashedAssetCacheHeaders(StaticFileResponseContext context)
{
    if (!context.Context.Request.Path.StartsWithSegments("/locales", StringComparison.OrdinalIgnoreCase))
    {
        return;
    }

    context.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
    {
        NoCache = true,
        MustRevalidate = true
    };
}

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
    diskProvider.EnsureFolder(PathHelper.FullManualsPath);
}

static async Task SeedConfig(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var configRepository = scope.ServiceProvider.GetRequiredService<IConfigRepository>();
    await configRepository.SeedConfigAsync(ConfigDefaults.All);
}

static async Task SeedAuthData(IServiceProvider serviceProvider, string? adminPassword)
{
    using var scope = serviceProvider.CreateScope();
    var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
    var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
    var seedLogger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
    await DbSeeder.SeedAuthData(roleManager, userManager, seedLogger, adminPassword);
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
