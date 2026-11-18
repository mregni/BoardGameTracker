using BoardGameTracker.Api.Infrastructure;
using BoardGameTracker.Common.Helpers;
using Microsoft.AspNetCore.StaticFiles;
using Microsoft.Extensions.FileProviders;
using Microsoft.Net.Http.Headers;
using Serilog;
using Serilog.Events;

namespace BoardGameTracker.Host.Startup;

internal static class PipelineSetup
{
    public static WebApplication UseAppRequestLogging(this WebApplication app)
    {
        app.UseSerilogRequestLogging(options =>
        {
            options.GetLevel = (context, _, exception) =>
                exception != null || context.Response.StatusCode >= StatusCodes.Status500InternalServerError
                    ? LogEventLevel.Error
                    : context.Request.Path.StartsWithSegments("/api/health")
                        ? LogEventLevel.Verbose
                        : LogEventLevel.Information;
        });
        return app;
    }

    public static WebApplication UseSecurityHeaders(this WebApplication app, bool hstsEnabled, bool swaggerEnabled)
    {
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
                    : "default-src 'self'; img-src 'self' data: blob: https:; script-src 'self'; style-src 'self' 'unsafe-inline' https://fonts.googleapis.com; font-src 'self' https://fonts.gstatic.com; connect-src 'self' https://*.ingest.sentry.io https://*.ingest.us.sentry.io; worker-src 'self' blob:; frame-ancestors 'none'; form-action 'self';";

                if (hstsEnabled && context.Request.IsHttps)
                {
                    headers["Strict-Transport-Security"] = "max-age=2592000";
                }

                return Task.CompletedTask;
            });

            await next();
        });
        return app;
    }

    public static WebApplication UseAppSwaggerUi(this WebApplication app)
    {
        app.UseSwagger();
        app.UseSwaggerUI(options =>
        {
            options.UseResponseInterceptor(
                "(res) => { try { if (res.status >= 200 && res.status < 300) { var body = res.obj || (res.text ? JSON.parse(res.text) : null); if (body && body.accessToken && window.ui) { window.ui.preauthorizeApiKey('Bearer', body.accessToken); console.log('[Swagger] Bearer token captured from auth response.'); } } } catch (e) { console.warn('[Swagger] auth interceptor failed', e); } return res; }");
        });
        return app;
    }

    public static WebApplication UseImageFiles(this WebApplication app)
    {
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
        return app;
    }

    public static WebApplication UseSpaFiles(this WebApplication app)
    {
        app.UseStaticFiles(new StaticFileOptions { OnPrepareResponse = SetStaticAssetCacheHeaders });
        app.MapFallback("/api/{**path}", () => Results.NotFound());
        app.MapFallbackToFile("index.html", new StaticFileOptions { OnPrepareResponse = SetIndexCacheHeaders })
            .WithMetadata(new HttpMethodMetadata([HttpMethods.Get, HttpMethods.Head]));
        return app;
    }

    private static void SetIndexCacheHeaders(StaticFileResponseContext context)
    {
        context.Context.Response.GetTypedHeaders().CacheControl = new CacheControlHeaderValue
        {
            NoCache = true,
            NoStore = true,
            MustRevalidate = true
        };
    }

    private static void SetStaticAssetCacheHeaders(StaticFileResponseContext context)
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
}
