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
using Microsoft.Extensions.Http;
using Microsoft.Net.Http.Headers;
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

builder.WebHost.UseConfiguredSentry();
builder.Host.UseContentRoot(Directory.GetCurrentDirectory());

builder.Host.UseSerilog();

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

app.UseSerilogRequestLogging();

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
logger.LogInformation("Server URLs: {Urls}", string.Join(", ", app.Urls));
logger.LogInformation("HTTP ports: {HttpPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTP_PORTS"));
logger.LogInformation("HTTPS ports: {HttpsPorts}", Environment.GetEnvironmentVariable("ASPNETCORE_HTTPS_PORTS"));
logger.LogInformation("Is Development: {IsDevelopment}", app.Environment.IsDevelopment());


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

SendStartApplicationCommand(app.Services);
RunDbMigrations(app.Services);

await app.RunAsync();

await Log.CloseAndFlushAsync();

static void RunDbMigrations(IServiceProvider serviceProvider)
{
    using var scope = serviceProvider.CreateScope();
    var context = scope.ServiceProvider.GetRequiredService<MainDbContext>();

    if (context == null)
    {
        throw new ServiceNotResolvedException("Can't resolve MainContext");
    }
    context.Database.Migrate();
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
    diskProvider.EnsureFolder(PathHelper.FullBadgeImagePath);
    diskProvider.EnsureFolder(PathHelper.FullCommonImagePath);
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
