using System.Net;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using Ardalis.GuardClauses;
using BoardGamer.BoardGameGeek.BoardGameGeekXmlApi2;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Configuration;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Auth;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.DockerHub;
using BoardGameTracker.Core.Extensions;
using BoardGameTracker.Core.Games;
using BoardGameTracker.Core.Images;
using BoardGameTracker.Core.Settings.Interfaces;
using BoardGameTracker.Core.Updates;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.DataProtection.KeyManagement;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using Microsoft.OpenApi;
using Npgsql;
using Refit;
using Serilog;
using Swashbuckle.AspNetCore.Swagger;

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
builder.Services.AddProblemDetails();
builder.Services.AddDataProtection()
    .PersistKeysToDbContext<MainDbContext>()
    .SetApplicationName("boardgametracker");

var environmentProvider = new EnvironmentProvider();
if (environmentProvider.DataProtectionKey is { } dataProtectionKey)
{
    var keyMaterial = new DataProtectionKeyMaterial(dataProtectionKey);
    builder.Services.AddSingleton(keyMaterial);
    builder.Services.Configure<KeyManagementOptions>(options => options.XmlEncryptor = new SecretXmlEncryptor(keyMaterial));
}
var trustedProxies = TrustedProxyList.Parse(environmentProvider.TrustedProxies);

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();

    foreach (var proxy in trustedProxies.Proxies)
    {
        options.KnownProxies.Add(proxy);
    }

    foreach (var network in trustedProxies.Networks)
    {
        options.KnownIPNetworks.Add(network);
    }
});

builder.Services.ConfigureHttpClientDefaults(httpClientBuilder =>
    httpClientBuilder.ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(30)));

builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequireDigit = false;
        options.Password.RequireLowercase = false;
        options.Password.RequireUppercase = false;
        options.Password.RequireNonAlphanumeric = false;
        options.Password.RequiredLength = 8;
        options.User.RequireUniqueEmail = false;
    })
    .AddRoles<IdentityRole>()
    .AddEntityFrameworkStores<MainDbContext>()
    .AddDefaultTokenProviders()
    .AddSignInManager();
builder.Services.AddHttpContextAccessor();

var authEnabled = environmentProvider.AuthEnabled;
var jwtSecret = environmentProvider.JwtSecret ?? builder.Configuration["Jwt:Secret"];
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

builder.Services.AddOptions<JwtOptions>()
    .Bind(builder.Configuration.GetSection(JwtOptions.SectionName))
    .PostConfigure(options => options.Secret = jwtSecret)
    .ValidateDataAnnotations()
    .ValidateOnStart();

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
    })
    .AddJwtBearer();
builder.Services.AddOptions<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme)
    .Configure<IOptions<JwtOptions>>((options, jwt) =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwt.Value.Issuer,
            ValidAudience = jwt.Value.Audience,
            ClockSkew = TimeSpan.FromSeconds(30),
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Value.Secret))
        };
    });

builder.Services.AddAuthorization();

builder.Services.AddRateLimiter(options =>
{
    options.AddPolicy("auth", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddressKey.From(context.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("changedetection", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddressKey.From(context.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("gamenight-link", context => RateLimitPartition.GetFixedWindowLimiter(
        ClientAddressKey.From(context.Connection.RemoteIpAddress),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 30,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.AddPolicy("rag", context => RateLimitPartition.GetFixedWindowLimiter(
        UserRateLimitKey.From(context),
        _ => new FixedWindowRateLimiterOptions
        {
            PermitLimit = 10,
            Window = TimeSpan.FromMinutes(1),
            QueueLimit = 0
        }));
    options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
    options.OnRejected = (context, _) =>
    {
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            context.HttpContext.Response.Headers.RetryAfter = Math.Ceiling(retryAfter.TotalSeconds).ToString(System.Globalization.CultureInfo.InvariantCulture);
        }

        var request = context.HttpContext.Request;
        if (HttpMethods.IsGet(request.Method) && request.Path.StartsWithSegments("/api/auth/oidc"))
        {
            context.HttpContext.Response.StatusCode = StatusCodes.Status302Found;
            context.HttpContext.Response.Headers.Location =
                $"{OidcController.CallbackPage}?error={Uri.EscapeDataString(BoardGameTracker.Common.Constants.Errors.TooManyRequests)}";
        }

        return ValueTask.CompletedTask;
    };
});

builder.Services.AddHttpClient();
builder.Services.AddHttpClient(BoardGameTracker.Core.Rag.AiClientFactory.HttpClientName)
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromMinutes(5));
builder.Services.AddHttpClient(BoardGameTracker.Core.ChangeDetection.ChangeDetectionClient.HttpClientName)
    .ConfigureHttpClient(client =>
    {
        client.Timeout = TimeSpan.FromSeconds(10);
        client.MaxResponseContentBufferSize = 64 * 1024;
    })
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient(OidcService.HttpClientName)
    .ConfigureHttpClient(client => client.MaxResponseContentBufferSize = 1024 * 1024)
    .ConfigurePrimaryHttpMessageHandler(() => new SocketsHttpHandler { AllowAutoRedirect = false });
builder.Services.AddHttpClient(ImageService.HttpClientName)
    .AddStandardResilienceHandler();
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

    options.SupportNonNullableReferenceTypes();

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
    })
    .AddStandardResilienceHandler();

var app = builder.Build();
if (args is ["--openapi", var openApiPath])
{
    await WriteOpenApiDocument(app.Services, openApiPath);
    await Log.CloseAndFlushAsync();
    return;
}

CreateFolders(app.Services);

app.UseSerilogRequestLogging(options =>
{
    options.GetLevel = (context, _, exception) =>
        exception != null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
            ? Serilog.Events.LogEventLevel.Error
            : context.Request.Path.StartsWithSegments("/api/health")
                ? Serilog.Events.LogEventLevel.Verbose
                : Serilog.Events.LogEventLevel.Information;
});

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
        headers["Permissions-Policy"] = "camera=(), microphone=(), geolocation=()";

        var isSwagger = swaggerEnabled && context.Request.Path.StartsWithSegments("/swagger");
        headers["Content-Security-Policy"] = isSwagger
            ? "default-src 'self'; img-src 'self' data: blob:; script-src 'self' 'unsafe-inline' 'unsafe-eval'; style-src 'self' 'unsafe-inline'; frame-ancestors 'none'; form-action 'self';"
            : "default-src 'self'; img-src 'self' data: blob: https:; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://*.ingest.sentry.io https://*.ingest.us.sentry.io; frame-ancestors 'none'; form-action 'self';";

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

app.UseAuthDisabledMiddleware();
app.UseAuthentication();
app.UseAuthorization();
app.UseRateLimiter();

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

app.UseWhen(
    context => context.Request.Path.StartsWithSegments(ProfileImageCookie.Path),
    branch => branch.UseMiddleware<ProfileImageAccessMiddleware>());
app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullProfileImagePath),
    RequestPath = ProfileImageCookie.Path,
    OnPrepareResponse = context => context.Context.Response.Headers.CacheControl = "private, no-cache"
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
if (!authEnabled)
{
    logger.LogWarning(
        "AUTH_ENABLED=false: every request is treated as an administrator. Anyone who can reach this port can change settings, store API keys, upload files, import games and use the AI assistant. Only use this on a trusted network or behind a proxy that authenticates for you");
}

if (!app.Environment.IsDevelopment())
{
    app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetStaticAssetCacheHeaders });
    app.MapFallback("/api/{**path}", () => Results.NotFound());
    app.MapFallbackToFile("index.html", new StaticFileOptions { OnPrepareResponse = SetIndexCacheHeaders })
        .WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
}

RunDbMigrations(app.Services);
await SeedConfig(app.Services);
if (authEnabled)
{
    await SeedAuthData(app.Services, environmentProvider.AdminPassword);
}

await app.RunAsync();

await Log.CloseAndFlushAsync();

static async Task WriteOpenApiDocument(IServiceProvider services, string path)
{
    var document = services.GetRequiredService<ISwaggerProvider>().GetSwagger("v1");
    var fullPath = Path.GetFullPath(path);
    Directory.CreateDirectory(Path.GetDirectoryName(fullPath)!);
    await using var writer = new StreamWriter(fullPath) { NewLine = "\n" };
    document.SerializeAsV3(new OpenApiJsonWriter(writer));
}

static void SetIndexCacheHeaders(StaticFileResponseContext context)
{
    context.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
    {
        NoCache = true,
        NoStore = true,
        MustRevalidate = true
    };
}

static void SetStaticAssetCacheHeaders(StaticFileResponseContext context)
{
    var path = context.Context.Request.Path;
    var headers = context.Context.Response.GetTypedHeaders();

    if (path.StartsWithSegments("/assets", StringComparison.OrdinalIgnoreCase))
    {
        headers.CacheControl = new CacheControlHeaderValue
        {
            Public = true,
            MaxAge = TimeSpan.FromDays(365),
            Extensions = { new NameValueHeaderValue("immutable") }
        };
        return;
    }

    if (path.StartsWithSegments("/locales", StringComparison.OrdinalIgnoreCase))
    {
        headers.CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            MustRevalidate = true
        };
    }
}

static void RunDbMigrations(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = Guard.Against.Null(scope.ServiceProvider.GetRequiredService<MainDbContext>());
    WaitForDatabase(context);
    EnsureVectorExtensionAvailable(context);
    context.Database.Migrate();
}

static void WaitForDatabase(MainDbContext context)
{
    var creator = context.Database.GetService<IRelationalDatabaseCreator>();
    const int attempts = 10;
    for (var attempt = 1; ; attempt++)
    {
        try
        {
            if (!creator.Exists())
            {
                Log.Information("Database {Database} does not exist yet; creating it", context.Database.GetDbConnection().Database);
                creator.Create();
            }

            return;
        }
        catch (Exception ex) when (ex is NpgsqlException or System.Net.Sockets.SocketException or TimeoutException)
        {
            if (attempt >= attempts)
            {
                throw new InvalidOperationException(
                    $"Could not open the PostgreSQL database after {attempts} attempts ({ex.Message}). Check DB_HOST, DB_PORT, DB_USER and DB_PASSWORD and make sure the database container is running.",
                    ex);
            }

            Log.Warning("Database not reachable yet (attempt {Attempt}/{Attempts}): {Message}", attempt, attempts, ex.Message);
        }

        Thread.Sleep(TimeSpan.FromSeconds(3));
    }
}

static void EnsureVectorExtensionAvailable(MainDbContext context)
{
    var available = context.Database
        .SqlQueryRaw<int>("SELECT 1 AS \"Value\" FROM pg_available_extensions WHERE name = 'vector'")
        .AsEnumerable()
        .Any();

    if (available)
    {
        return;
    }

    const string message =
        "The PostgreSQL server does not provide the 'vector' extension, which BoardGameTracker requires. " +
        "Use the pgvector/pgvector:pg16 image instead of postgres:16 (your existing data folder keeps working), " +
        "or install pgvector on your own server. See the Upgrading page in the documentation.";
    Log.Fatal(message);
    throw new InvalidOperationException(message);
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
    serializerSettings.WriteIndented = EnvironmentExtensions.IsDevelopment();

    // Ensure all DateTime values are handled as UTC
    serializerSettings.Converters.Add(new UtcDateTimeConverter());
    serializerSettings.Converters.Add(new UtcNullableDateTimeConverter());
    serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase));
}

public partial class Program;
