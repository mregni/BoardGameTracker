using System.Text.Json;
using System.Text.Json.Serialization;
using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Common.Helpers;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Mvc.ApplicationParts;
using Microsoft.OpenApi;

namespace BoardGameTracker.Host.Startup;

internal static class ApiSetup
{
    public static IServiceCollection AddTrustedProxies(this IServiceCollection services, TrustedProxyList trustedProxies)
    {
        return services.Configure<ForwardedHeadersOptions>(options =>
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
    }

    public static IServiceCollection AddAppCors(this IServiceCollection services, IEnvironmentProvider environmentProvider)
    {
        var corsOrigins = environmentProvider.CorsOrigins;
        return services.AddCors(options =>
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
    }

    public static IServiceCollection AddAppControllers(this IServiceCollection services)
    {
        var mvcBuilder = services
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
        return services;
    }

    public static IServiceCollection AddAppSwagger(this IServiceCollection services, string version)
    {
        services.AddEndpointsApiExplorer();
        return services.AddSwaggerGen(options =>
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
    }

    private static void ApplySerializerSettings(JsonSerializerOptions serializerSettings)
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
}
