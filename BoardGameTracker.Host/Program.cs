using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exeptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Bgg;
using BoardGameTracker.Core.Commands;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Disk.Interfaces;
using BoardGameTracker.Core.Extensions;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Refit;

var logLevel = LogLevelExtensions.GetEnvironmentLogLevel();

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddCoreService();

builder.WebHost.UseConfiguredSentry();
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());

builder.Services.AddLogging(b =>
{
    b.ClearProviders();
    b.SetMinimumLevel(logLevel);
    b.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    b.AddFilter("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.Error);
    b.AddConsole();
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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

builder.Services.AddMediatR(opt =>
{
    opt.RegisterServicesFromAssemblyContaining<ApplicationStartedCommand>();
});

builder.Services
    .AddControllers(options =>
    {
        options.ReturnHttpNotAcceptable = true;
    })
    .AddJsonOptions(options =>
    {
        ApplySerializerSettings(options.JsonSerializerOptions);
    })
    .AddControllersAsServices();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddAutoMapper(typeof(MapProfiles));

var refitSettings = new RefitSettings
{
    ContentSerializer = new XmlContentSerializer()
};
builder.Services.AddRefitClient<IBggApi>(refitSettings)
    .ConfigureHttpClient(options =>
    {
        options.BaseAddress = new Uri("https://boardgamegeek.com/xmlapi2");
    });

builder.Services.AddSpaStaticFiles(configuration => {
    configuration.RootPath = "wwwroot";
});

var app = builder.Build();
CreateFolders(app.Services);

app.UseRouting();
app.MapControllers();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("Allow");
app.UseSentryTracing();

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullCoverImagePath),
    RequestPath = "/images/cover"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullBackgroundImagePath),
    RequestPath = "/images/background"
});

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(PathHelper.FullProfileImagePath),
    RequestPath = "/images/profile"
});

var logger = app.Services.GetService<ILogger<Program>>();
logger.LogError("TEST");
logger.LogError(Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"));
logger.LogError(Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"));
logger.LogError(app.Environment.IsDevelopment().ToString());

if (!app.Environment.IsDevelopment())
{
    logger.LogError("PRODUCTION");
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

SendStartApplicationCommand(app.Services);
await RunDbMigrations(app.Services);

await app.RunAsync();

static Task RunDbMigrations(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

    if (context == null)
    {
        throw new ServiceNotResolvedException("Can't resolve MainContext");
    }
    return context.Database.MigrateAsync();
}

static void CreateFolders(IServiceProvider serviceProvider)
{
    var diskProvider = serviceProvider.GetService<IDiskProvider>();
    if (diskProvider == null)
    {
        throw new ServiceNotResolvedException("Can't resolve IDiskProvider");
    }
    diskProvider.EnsureFolder(PathHelper.FullConfigFilePath);
    diskProvider.EnsureFolder(PathHelper.FullRootImagePath);
    diskProvider.EnsureFolder(PathHelper.FullCoverImagePath);
    diskProvider.EnsureFolder(PathHelper.FullProfileImagePath);
    diskProvider.EnsureFolder(PathHelper.FullBackgroundImagePath);
}

static void SendStartApplicationCommand(IServiceProvider serviceProvider)
{
    var mediator = serviceProvider.GetService<IMediator>();
    if (mediator == null)
    {
        throw new ServiceNotResolvedException("Can't resolve IMediator");
    }
    mediator.Send(new ApplicationStartedCommand());
}

static void ApplySerializerSettings(JsonSerializerOptions serializerSettings)
{
    serializerSettings.AllowTrailingCommas = true;
    serializerSettings.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
    serializerSettings.PropertyNameCaseInsensitive = true;
    serializerSettings.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
    serializerSettings.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    serializerSettings.WriteIndented = true;
}