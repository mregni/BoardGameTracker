using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Models.Charts;

namespace BoardGameTracker.Common.Extensions;

public static class DashboardDtoExtensions
{
    public static RecentActivityDto ToDto(this Session session)
    {
        var winner = session.PlayerSessions.FirstOrDefault(ps => ps.Won);
        return new RecentActivityDto
        {
            Id = session.Id,
            GameId = session.GameId,
            GameTitle = session.Game.Title,
            GameImage = session.Game.Image,
            Start = session.Start,
            PlayerCount = session.PlayerSessions.Count,
            DurationInMinutes = session.GetDuration().TotalMinutes,
            WinnerId = winner?.PlayerId,
            WinnerName = winner?.Player?.Name
        };
    }

    public static List<RecentActivityDto> ToDtoList(this IEnumerable<Session> sessions)
    {
        return sessions.Select(ToDto).ToList();
    }

    public static GameStateChart ToGameStateChart(this IGrouping<GameState, Game> grouping)
    {
        return new GameStateChart
        {
            Type = grouping.Key,
            GameCount = grouping.Count()
        };
    }

    public static List<GameStateChart> ToDtoList(this IEnumerable<IGrouping<GameState, Game>> groupings)
    {
        return groupings.Select(g => g.ToGameStateChart()).ToList();
    }

    public static MostPlayedGameDto ToMostPlayedGameDto(this (int GameId, string Title, string? Image, int PlayCount) tuple)
    {
        return new MostPlayedGameDto
        {
            Id = tuple.GameId,
            Title = tuple.Title,
            Image = tuple.Image,
            TotalSessions = tuple.PlayCount
        };
    }

    public static List<MostPlayedGameDto> ToDtoList(
        this IEnumerable<(int GameId, string Title, string? Image, int PlayCount)> tuples)
    {
        return tuples.Select(t => t.ToMostPlayedGameDto()).ToList();
    }

    public static DashboardTopPlayerDto ToDashboardTopPlayerDto(
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

    public static List<DashboardTopPlayerDto> ToDtoList(
        this IEnumerable<(int Id, string Name, string? Image, int PlayCount, int WinCount)> tuples)
    {
        return tuples.Select(t => t.ToDashboardTopPlayerDto()).ToList();
    }

    public static RecentGameDto ToRecentGameDto(this Game game)
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

    public static List<RecentGameDto> ToDtoList(this IEnumerable<Game> games)
    {
        return games.Select(g => g.ToRecentGameDto()).ToList();
    }

    public static PlayByDay ToPlayByDay(this IGrouping<DayOfWeek, Session> grouping)
    {
        return new PlayByDay
        {
            DayOfWeek = grouping.Key,
            PlayCount = grouping.Count()
        };
    }

    public static List<PlayByDay> ToDtoList(this IEnumerable<IGrouping<DayOfWeek, Session>> groupings)
    {
        return groupings.Select(g => g.ToPlayByDay()).ToList();
    }
}