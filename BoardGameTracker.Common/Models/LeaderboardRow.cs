namespace BoardGameTracker.Common.Models;

public record LeaderboardRow(int PlayerId, string Name, string? Image, int PlayCount, int WinCount, int PodiumCount, double MinutesPlayed);
