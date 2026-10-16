using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.Extensions;

public static class DashboardDtoExtensions
{
    private static RecentActivityDto ToRecentActivityDto(this Session session)
    {
        return new RecentActivityDto
        {
            Id = session.Id,
            GameId = session.GameId,
            GameTitle = session.Game.Title,
            GameImage = session.Game.Image,
            Start = session.Start,
            PlayerCount = session.PlayerSessions.Count,
            DurationInMinutes = session.GetDuration().TotalMinutes,
            Winners = session.PlayerSessions
                .Where(ps => !session.IsCoop && ps.Won)
                .Select(ps => new DashboardWinnerDto
                {
                    Id = ps.PlayerId,
                    Name = ps.Player?.Name ?? string.Empty,
                    Image = ps.Player?.Image
                })
                .ToList(),
            IsCoop = session.IsCoop,
            TeamWon = session.TeamWon
        };
    }

    public static DashboardShelfOfShameDto ToDashboardDto(this ShameStatistics statistics)
    {
        return new DashboardShelfOfShameDto
        {
            Count = statistics.Count,
            NeverPlayedCount = statistics.NeverPlayedCount,
            TotalValue = statistics.TotalValue
        };
    }

    public static List<RecentActivityDto> ToRecentActivityListDto(this IEnumerable<Session> sessions)
    {
        return sessions.Select(ToRecentActivityDto).ToList();
    }

    private static MostPlayedGameDto ToMostPlayedGameDto(this (int GameId, string Title, string? Image, int PlayCount) tuple)
    {
        return new MostPlayedGameDto
        {
            Id = tuple.GameId,
            Title = tuple.Title,
            Image = tuple.Image,
            TotalSessions = tuple.PlayCount
        };
    }

    private static DashboardTopPlayerDto ToDashboardTopPlayerDto(
        this (int Id, string Name, string? Image, int PlayCount, int WinCount) tuple)
    {
        return new DashboardTopPlayerDto
        {
            Id = tuple.Id,
            Name = tuple.Name,
            Image = tuple.Image,
            PlayCount = tuple.PlayCount,
            WinCount = tuple.WinCount
        };
    }

    private static RecentGameDto ToRecentAddedGameDto(this Game game)
    {
        return new RecentGameDto
        {
            Id = game.Id,
            Title = game.Title,
            Image = game.Image,
            AdditionDate = game.AdditionDate,
            Price = game.BuyingPrice?.Amount
        };
    }

    public static List<RecentGameDto> ToRecentAddedGameListDto(this IEnumerable<Game> games)
    {
        return games.Select(g => g.ToRecentAddedGameDto()).ToList();
    }

    public static List<DashboardTopPlayerDto> ToListDto(
        this IEnumerable<(int Id, string Name, string? Image, int PlayCount, int WinCount)> tuples)
    {
        return tuples.Select(t => t.ToDashboardTopPlayerDto()).ToList();
    }
    
    public static List<MostPlayedGameDto> ToListDto(
        this IEnumerable<(int GameId, string Title, string? Image, int PlayCount)> tuples)
    {
        return tuples.Select(t => t.ToMostPlayedGameDto()).ToList();
    }
}