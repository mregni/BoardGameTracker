using System.Text.RegularExpressions;
using Sentry;

namespace BoardGameTracker.Common.Helpers;

public static partial class SentryScrubber
{
    public const string Filtered = "[Filtered]";

    [GeneratedRegex(
        @"(?<prefix>^|[?&#;])(?<name>code|state|token|id_token|access_token|refresh_token|session_state|linkid|[^=&#?;\s/]*(?:password|secret|key)[^=&#?;\s/]*)=(?<value>[^&#\s""']*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitiveParameterRegex();

    [GeneratedRegex(
        @"(?<path>/gamenight/link/|/manual/gamenight/)[0-9a-f]{8}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{4}-[0-9a-f]{12}",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex SensitivePathRegex();

    public static string? Scrub(string? value)
    {
        if (string.IsNullOrEmpty(value))
        {
            return value;
        }

        var scrubbed = SensitiveParameterRegex().Replace(value, m => $"{m.Groups["prefix"].Value}{m.Groups["name"].Value}={Filtered}");
        return SensitivePathRegex().Replace(scrubbed, m => $"{m.Groups["path"].Value}{Filtered}");
    }

    public static SentryEvent ScrubEvent(SentryEvent @event)
    {
        ScrubRequest(@event.Request);
        @event.TransactionName = Scrub(@event.TransactionName);
        if (@event.Message is { } message)
        {
            message.Message = Scrub(message.Message);
            message.Formatted = Scrub(message.Formatted);
        }

        foreach (var (key, value) in @event.Tags.ToList())
        {
            @event.SetTag(key, Scrub(value) ?? string.Empty);
        }

        return @event;
    }

    public static SentryTransaction? ScrubTransaction(SentryTransaction transaction)
    {
        if (!string.Equals(Scrub(transaction.Name), transaction.Name, StringComparison.Ordinal))
        {
            return null;
        }

        ScrubRequest(transaction.Request);

        foreach (var (key, value) in transaction.Tags.ToList())
        {
            transaction.SetTag(key, Scrub(value) ?? string.Empty);
        }

        foreach (var span in transaction.Spans)
        {
            span.Description = Scrub(span.Description);
            foreach (var (key, value) in span.Data.ToList())
            {
                if (value is string text)
                {
                    span.SetData(key, Scrub(text));
                }
            }
        }

        return transaction;
    }

    public static Breadcrumb ScrubBreadcrumb(Breadcrumb breadcrumb)
    {
        var message = Scrub(breadcrumb.Message);
        var data = breadcrumb.Data?.ToDictionary(x => x.Key, x => Scrub(x.Value) ?? string.Empty);
        var changed = !string.Equals(message, breadcrumb.Message, StringComparison.Ordinal)
                      || (data != null && data.Any(x => !string.Equals(x.Value, breadcrumb.Data![x.Key], StringComparison.Ordinal)));
        if (!changed)
        {
            return breadcrumb;
        }

        return new Breadcrumb(
            message ?? string.Empty,
            breadcrumb.Type ?? "default",
            data,
            breadcrumb.Category,
            breadcrumb.Level);
    }

    private static void ScrubRequest(SentryRequest request)
    {
        request.Url = Scrub(request.Url);
        request.QueryString = Scrub(request.QueryString);
        foreach (var (key, value) in request.Headers.ToList())
        {
            request.Headers[key] = Scrub(value) ?? string.Empty;
        }
    }
}
