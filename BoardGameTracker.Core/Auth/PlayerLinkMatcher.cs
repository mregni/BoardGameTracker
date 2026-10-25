using System.Globalization;
using System.Text;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Core.Auth;

public record PlayerLinkCandidate(int Id, string Name, string? Image, string? Email);

public static class PlayerLinkMatcher
{
    public const int MaxSuggestions = 3;
    private const int MinimumWordLength = 2;
    private static readonly char[] WordSeparators = [' ', '\t', '\r', '\n', '.', '_', '-', ','];

    public static List<PlayerLinkSuggestionDto> Suggest(IEnumerable<PlayerLinkCandidate> players, string? email, string? displayName, string? userName)
    {
        var accountEmail = NormalizeEmail(email);
        var displayWords = Words(displayName);
        var userWords = Words(LocalPart(userName));

        return players
            .Select(player => (Player: player, Rank: Rank(player, accountEmail, displayWords, userWords)))
            .Where(x => x.Rank.HasValue)
            .OrderBy(x => x.Rank!.Value.Score)
            .ThenBy(x => x.Player.Name, StringComparer.OrdinalIgnoreCase)
            .ThenBy(x => x.Player.Id)
            .Take(MaxSuggestions)
            .Select(x => new PlayerLinkSuggestionDto(x.Player.Id, x.Player.Name, x.Player.Image, x.Rank!.Value.Reason))
            .ToList();
    }

    public static bool EmailAllowsLink(string? accountEmail, string? playerEmail)
    {
        var player = NormalizeEmail(playerEmail);
        return player == null || player == NormalizeEmail(accountEmail);
    }

    private static (int Score, PlayerLinkMatchReason Reason)? Rank(PlayerLinkCandidate player, string? accountEmail, string[] displayWords, string[] userWords)
    {
        if (accountEmail != null && accountEmail == NormalizeEmail(player.Email))
        {
            return (0, PlayerLinkMatchReason.Email);
        }

        var playerWords = Words(player.Name);
        if (playerWords.Length == 0)
        {
            return null;
        }

        if (SameWords(playerWords, displayWords))
        {
            return (1, PlayerLinkMatchReason.Name);
        }

        if (SameWords(playerWords, userWords))
        {
            return (2, PlayerLinkMatchReason.Name);
        }

        if (PartlyMatches(playerWords, displayWords))
        {
            return (3, PlayerLinkMatchReason.Partial);
        }

        if (PartlyMatches(playerWords, userWords))
        {
            return (4, PlayerLinkMatchReason.Partial);
        }

        return null;
    }

    private static bool SameWords(string[] left, string[] right) =>
        right.Length > 0 && left.AsSpan().SequenceEqual(right);

    private static bool PartlyMatches(string[] left, string[] right)
    {
        if (left.Length == 0 || right.Length == 0)
        {
            return false;
        }

        if (left[0] == right[0] && left[0].Length >= MinimumWordLength)
        {
            return true;
        }

        var (shorter, longer) = left.Length <= right.Length ? (left, right) : (right, left);
        if (shorter.Length == longer.Length || shorter.Sum(w => w.Length) < MinimumWordLength)
        {
            return false;
        }

        for (var start = 0; start + shorter.Length <= longer.Length; start++)
        {
            if (longer.AsSpan(start, shorter.Length).SequenceEqual(shorter))
            {
                return true;
            }
        }

        return false;
    }

    private static string? NormalizeEmail(string? email)
    {
        var trimmed = email?.Trim();
        return string.IsNullOrEmpty(trimmed) ? null : trimmed.ToLowerInvariant();
    }

    private static string? LocalPart(string? userName)
    {
        if (userName == null)
        {
            return null;
        }

        var at = userName.IndexOf('@');
        return at > 0 ? userName[..at] : userName;
    }

    private static string[] Words(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            return [];
        }

        return RemoveAccents(value)
            .ToLowerInvariant()
            .Split(WordSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
    }

    private static string RemoveAccents(string value)
    {
        var decomposed = value.Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var c in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(c);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
