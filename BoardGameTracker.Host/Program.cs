using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Api.Controllers;
using BoardGameTracker.Common.Exeptions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Commands;
using MediatR;
using Microsoft.AspNetCore.HttpOverrides;

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
            policyBuilder.AllowAnyOrigin()
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

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

builder.Services.AddSpaStaticFiles(configuration =>
{
    configuration.RootPath =Path.Combine("..", "BoardGameTracker.Web", "ClientApp");
});

var app = builder.Build();

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

app.UseSpaStaticFiles();

app.UseRouting();

app.UseEndpoints(endpoints =>
{
    endpoints.MapControllers();
});

app.UseSpa(spa =>
{
    spa.Options.SourcePath = "ClientApp";
});

var mediator = app.Services.GetService<IMediator>();
if (mediator == null)
{
    throw new ServiceNotResolvedException("Can't resolve IMediator");
}
mediator.Publish(new ApplicationStartedCommand());

app.Run();

static void SetConfiguration(WebApplicationBuilder builder)
{
    var configPath = PathHelper.ConfigFilePath;
    try
    {
        builder.Configuration
            .AddXmlFile(configPath, optional: true, reloadOnChange: false)
            .AddEnvironmentVariables()
            .Build();
    }
    catch (InvalidDataException ex)
    {
        throw new InvalidConfigFileException($"{configPath} is corrupt or invalid. Please delete the config file and Radarr will recreate it.", ex);
    }
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