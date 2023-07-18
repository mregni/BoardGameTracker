using System.Data;
using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Exeptions;
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
using Refit;

var builder = WebApplication.CreateBuilder(args);

builder.Host.UseContentRoot(Directory.GetCurrentDirectory());

SetConfiguration(builder);

builder.Services.AddLogging(b =>
{
    b.ClearProviders();
    b.SetMinimumLevel(LogLevel.Trace);
    b.AddFilter("Microsoft.AspNetCore", LogLevel.Warning);
    b.AddFilter("Microsoft.AspNetCore.DataProtection.KeyManagement.XmlKeyManager", LogLevel.Error);
});

builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto;
    options.KnownNetworks.Clear();
    options.KnownProxies.Clear();
});

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
    .AddApplicationPart(typeof(GameController).Assembly)
    .AddJsonOptions(options =>
    {
        ApplySerializerSettings(options.JsonSerializerOptions);
    })
    .AddControllersAsServices();

builder.Services.AddCoreService();

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

builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath =Path.Combine("..", "BoardGameTracker.Web", "ClientApp");
});

var app = builder.Build();

CreateFolders(app.Services);

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
    app.UseDeveloperExceptionPage();
}
else
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
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

app.UseSpaStaticFiles();
app.UseCors("Allow");
app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});

SendStartApplicationCommand(app.Services);
await RunDbMigrations(app.Services);

app.Run();

static void SetConfiguration(WebApplicationBuilder builder)
{
    var configFile = PathHelper.FullConfigFile;
    try
    {
        builder.Configuration
            .AddXmlFile(configFile, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }
    catch (InvalidDataException ex)
    {
        throw new InvalidConfigFileException($"{configFile} is corrupt or invalid. Please delete the config file and Radarr will recreate it.", ex);
    }
}

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

    serializerSettings.Converters.Add(new JsonStringEnumConverter(JsonNamingPolicy.CamelCase, true));
}