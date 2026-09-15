using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Api.Infrastructure;

public class UntrustedProxyWarningMiddleware
{
    private const string ForwardedForHeader = "X-Forwarded-For";
    private static int _warned;

    private readonly RequestDelegate _next;
    private readonly ILogger<UntrustedProxyWarningMiddleware> _logger;

    public UntrustedProxyWarningMiddleware(RequestDelegate next, ILogger<UntrustedProxyWarningMiddleware> logger)
    {
        _next = next;
        _logger = logger;
    }

    public Task InvokeAsync(HttpContext context)
    {
        if (context.Request.Headers.ContainsKey(ForwardedForHeader) && Interlocked.Exchange(ref _warned, 1) == 0)
        {
            _logger.LogWarning(
                "Requests arrive with an {Header} header from {RemoteIp}, but TRUSTED_PROXIES is empty. Client IPs, HTTPS detection and the login rate limit are based on the proxy address. Set TRUSTED_PROXIES to the proxy IP or network (for example 172.16.0.0/12 for Docker)",
                ForwardedForHeader,
                context.Connection.RemoteIpAddress);
        }

        return _next(context);
    }
}
