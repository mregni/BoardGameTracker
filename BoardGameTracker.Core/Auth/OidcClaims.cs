using System.Text.Json;

namespace BoardGameTracker.Core.Auth;

public sealed class OidcClaims
{
    public const string SubjectClaim = "sub";

    private readonly Dictionary<string, JsonElement> _claims;

    private OidcClaims(Dictionary<string, JsonElement> claims)
    {
        _claims = claims;
    }

    public static OidcClaims From(JsonElement source)
    {
        var claims = new Dictionary<string, JsonElement>(StringComparer.Ordinal);
        if (source.ValueKind == JsonValueKind.Object)
        {
            foreach (var property in source.EnumerateObject())
            {
                claims[property.Name] = property.Value.Clone();
            }
        }

        return new OidcClaims(claims);
    }

    public static OidcClaims Parse(string json)
    {
        using var document = JsonDocument.Parse(json);
        return From(document.RootElement);
    }

    public string? Subject => GetValue(SubjectClaim);

    public OidcClaims MergeWith(OidcClaims other)
    {
        var merged = new Dictionary<string, JsonElement>(_claims, StringComparer.Ordinal);
        foreach (var (name, value) in other._claims.Where(c => c.Key != SubjectClaim))
        {
            merged[name] = value;
        }

        return new OidcClaims(merged);
    }

    public string? GetValue(string claimType)
    {
        if (!_claims.TryGetValue(claimType, out var value))
        {
            return null;
        }

        return value.ValueKind == JsonValueKind.Array
            ? string.Join(",", value.EnumerateArray().Select(AsText))
            : AsText(value);
    }

    public List<string> GetValues(string claimType)
    {
        if (!_claims.TryGetValue(claimType, out var value))
        {
            return [];
        }

        if (value.ValueKind == JsonValueKind.Array)
        {
            return value.EnumerateArray()
                .Select(AsText)
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .Select(v => v!.Trim())
                .ToList();
        }

        var scalar = AsText(value);
        if (string.IsNullOrWhiteSpace(scalar))
        {
            return [];
        }

        return scalar
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();
    }

    private static string? AsText(JsonElement value)
    {
        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Null or JsonValueKind.Undefined => null,
            _ => value.GetRawText(),
        };
    }
}
