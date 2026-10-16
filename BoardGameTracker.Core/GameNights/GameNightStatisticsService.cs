using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Models;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Configuration.Interfaces;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.GameNights.Interfaces;
using BoardGameTracker.Core.GameNights.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.GameNights;

public class GameNightStatisticsService : IGameNightStatisticsService
{
    private const int RecentNightCount = 12;
    private const int MinimumStreak = 2;

    private readonly IReadRepository<GameNight> _gameNightRepository;
    private readonly IConfigRepository _configRepository;
    private readonly IDateTimeProvider _dateTimeProvider;
    private readonly ILogger<GameNightStatisticsService> _logger;

    public GameNightStatisticsService(
        IReadRepository<GameNight> gameNightRepository,
        IConfigRepository configRepository,
        IDateTimeProvider dateTimeProvider,
        ILogger<GameNightStatisticsService> logger)
    {
        _gameNightRepository = gameNightRepository;
        _configRepository = configRepository;
        _dateTimeProvider = dateTimeProvider;
        _logger = logger;
    }

    public async Task<GameNightStatisticsDto> GetStatistics(GameNightStatisticsPeriod period, CancellationToken cancellationToken = default)
    {
        _logger.LogDebug("Fetching game night statistics for {Period}", period);
        if (!await _configRepository.GetConfigValueOrDefaultAsync(Constants.AppConfig.GameNightsEnabled, true))
        {
            throw new FeatureDisabledException(Constants.AppConfig.GameNightsEnabled);
        }

        var now = _dateTimeProvider.UtcNow;
        var nights = await _gameNightRepository.ListAsync(new GameNightStatisticsSpec(GetPeriodStart(period, now), now), cancellationToken);

        var held = nights.Where(x => IsHeld(x, now)).ToList();
        var outcomes = held
            .Where(x => x.Sessions.Count > 0 || x.EndedAt.HasValue)
            .OrderBy(x => x.StartDate)
            .ThenBy(x => x.Id)
            .Select(x => new NightOutcome(x, NightWinnerRule.Decide(x.Sessions.SelectMany(s => s.Results))))
            .ToList();
        var played = outcomes.Where(x => x.Night.Sessions.Count > 0).ToList();
        var recent = outcomes.TakeLast(RecentNightCount).Select(x => x.Night).ToList();
        var winnerStreaks = GameNightStreaks.Find(played.Select(x => new StreakStep(x.Night.StartDate, x.Winners.WinnerIds)).ToList());

        return new GameNightStatisticsDto
        {
            Period = period,
            TotalNights = held.Count,
            NightsWithSessions = played.Count,
            Nights = outcomes.Count,
            GamesPlayed = outcomes.Sum(x => x.Night.Sessions.Count),
            TotalMinutes = outcomes.Sum(x => x.Night.Sessions.Sum(s => (s.End - s.Start).TotalMinutes)),
            AverageTable = outcomes.Count == 0 ? 0 : outcomes.Average(x => x.Night.AttendeeIds.Count),
            AverageDaysBetweenNights = outcomes.Count < 2
                ? null
                : (outcomes[^1].Night.StartDate - outcomes[0].Night.StartDate).TotalDays / (outcomes.Count - 1),
            TiedNights = played.Count(x => x.Winners.WinnerIds.Count > 1),
            Winners = BuildWinners(outcomes),
            History = BuildHistory(outcomes, played, winnerStreaks),
            Games = BuildGames(outcomes),
            PlannedNeverPlayed = BuildPlannedNeverPlayed(outcomes),
            Streaks = BuildStreaks(outcomes, played, winnerStreaks),
            RecentNights = recent.Select(ToReference).ToList(),
            Attendance = BuildAttendance(outcomes, recent),
            Records = BuildRecords(played)
        };
    }

    private DateTime? GetPeriodStart(GameNightStatisticsPeriod period, DateTime now)
    {
        return period switch
        {
            GameNightStatisticsPeriod.ThisYear => _dateTimeProvider.ConvertToUtc(
                new DateTime(_dateTimeProvider.ConvertToLocalTime(now).Year, 1, 1, 0, 0, 0, DateTimeKind.Unspecified)),
            GameNightStatisticsPeriod.Last12Months => now.AddMonths(-12),
            _ => null
        };
    }

    private static bool IsHeld(GameNightStatisticsNight night, DateTime now)
    {
        return night.Sessions.Count > 0 || night.EndedAt.HasValue || night.StartDate.Add(GameNight.PlayWindow) <= now;
    }

    private static bool WasThere(NightOutcome outcome, int playerId)
    {
        return outcome.Night.AttendeeIds.Contains(playerId) || outcome.Winners.WinnerIds.Contains(playerId);
    }

    private static List<NightWinnerStatisticDto> BuildWinners(List<NightOutcome> outcomes)
    {
        var winners = outcomes
            .SelectMany(x => x.Winners.WinnerIds)
            .Distinct()
            .Select(playerId => new NightWinnerStatisticDto
            {
                PlayerId = playerId,
                NightsWon = outcomes.Count(x => x.Winners.WinnerIds.Contains(playerId)),
                NightsAttended = outcomes.Count(x => WasThere(x, playerId)),
                GamesWon = outcomes.Sum(x => x.Night.Sessions.Count(s => s.Results.Any(r => r.PlayerId == playerId && r.Won)))
            })
            .OrderByDescending(x => x.NightsWon)
            .ThenByDescending(x => (double)x.NightsWon / x.NightsAttended)
            .ThenByDescending(x => x.GamesWon)
            .ThenBy(x => x.PlayerId)
            .ToList();

        for (var i = 0; i < winners.Count; i++)
        {
            winners[i].Rank = i > 0 && winners[i].NightsWon == winners[i - 1].NightsWon ? winners[i - 1].Rank : i + 1;
        }

        return winners;
    }

    private static List<NightHistoryDto> BuildHistory(List<NightOutcome> outcomes, List<NightOutcome> played, StreakResult winnerStreaks)
    {
        var streakPerNight = played
            .Select((x, index) => (x.Night.Id, Length: winnerStreaks.LengthPerStep[index]))
            .ToDictionary(x => x.Id, x => x.Length);

        return outcomes
            .AsEnumerable()
            .Reverse()
            .Select(x => new NightHistoryDto
            {
                GameNightId = x.Night.Id,
                Title = x.Night.Title,
                StartDate = x.Night.StartDate,
                LocationId = x.Night.LocationId,
                GamesPlayed = x.Night.Sessions.Count,
                MostWins = x.Winners.MostWins,
                WinnerIds = x.Winners.WinnerIds,
                WinnerStreak = streakPerNight.GetValueOrDefault(x.Night.Id)
            })
            .ToList();
    }

    private static List<NightGameStatisticDto> BuildGames(List<NightOutcome> outcomes)
    {
        return outcomes
            .SelectMany(x => x.Night.Sessions.Select(s => (NightId: x.Night.Id, s.GameId)))
            .GroupBy(x => x.GameId)
            .Select(g => new NightGameStatisticDto
            {
                GameId = g.Key,
                TimesPlayed = g.Count(),
                Nights = g.Select(x => x.NightId).Distinct().Count(),
                TimesPlanned = outcomes.Count(x => x.Night.PlannedGameIds.Contains(g.Key))
            })
            .OrderByDescending(x => x.TimesPlayed)
            .ThenByDescending(x => x.Nights)
            .ThenBy(x => x.GameId)
            .ToList();
    }

    private static List<PlannedGameStatisticDto> BuildPlannedNeverPlayed(List<NightOutcome> outcomes)
    {
        var playedGameIds = outcomes.SelectMany(x => x.Night.Sessions.Select(s => s.GameId)).ToHashSet();
        return outcomes
            .SelectMany(x => x.Night.PlannedGameIds.Distinct())
            .Where(x => !playedGameIds.Contains(x))
            .GroupBy(x => x)
            .Select(g => new PlannedGameStatisticDto { GameId = g.Key, TimesPlanned = g.Count() })
            .OrderByDescending(x => x.TimesPlanned)
            .ThenBy(x => x.GameId)
            .ToList();
    }

    private static GameNightStreaksDto BuildStreaks(List<NightOutcome> outcomes, List<NightOutcome> played, StreakResult winnerStreaks)
    {
        var attendance = GameNightStreaks.Find(outcomes.Select(x => new StreakStep(x.Night.StartDate, x.Night.AttendeeIds)).ToList());
        var games = GameNightStreaks.Find(played.Select(x => new StreakStep(x.Night.StartDate, x.Night.Sessions.Select(s => s.GameId).ToList())).ToList());

        return new GameNightStreaksDto
        {
            CurrentWinner = ToPlayerStreak(Longest(winnerStreaks.Runs.Where(x => x.IsCurrent))),
            LongestWinner = ToPlayerStreak(Longest(winnerStreaks.Runs)),
            CurrentAttendance = ToPlayerStreak(Longest(attendance.Runs.Where(x => x.IsCurrent))),
            LongestGame = Longest(games.Runs) is { } game
                ? new GameStreakDto { GameId = game.Key, Length = game.Length, From = game.From, To = game.To, IsCurrent = game.IsCurrent }
                : null
        };
    }

    private static StreakRun? Longest(IEnumerable<StreakRun> runs)
    {
        return runs
            .Where(x => x.Length >= MinimumStreak)
            .OrderByDescending(x => x.Length)
            .ThenByDescending(x => x.To)
            .ThenBy(x => x.Key)
            .FirstOrDefault();
    }

    private static PlayerStreakDto? ToPlayerStreak(StreakRun? run)
    {
        return run == null
            ? null
            : new PlayerStreakDto { PlayerId = run.Key, Length = run.Length, From = run.From, To = run.To };
    }

    private static List<NightAttendanceDto> BuildAttendance(List<NightOutcome> outcomes, List<GameNightStatisticsNight> recent)
    {
        return outcomes
            .SelectMany(x => x.Night.AttendeeIds)
            .Distinct()
            .Select(playerId => new NightAttendanceDto
            {
                PlayerId = playerId,
                NightsAttended = outcomes.Count(x => x.Night.AttendeeIds.Contains(playerId)),
                NightsHosted = outcomes.Count(x => x.Night.HostId == playerId),
                RecentNights = recent.Select(x => x.AttendeeIds.Contains(playerId)).ToList()
            })
            .OrderByDescending(x => x.NightsAttended)
            .ThenByDescending(x => x.NightsHosted)
            .ThenBy(x => x.PlayerId)
            .ToList();
    }

    private GameNightRecordsDto BuildRecords(List<NightOutcome> played)
    {
        var longest = played
            .Select(x => (Outcome: x, Minutes: (x.Night.Sessions.Max(s => s.End) - x.Night.Sessions.Min(s => s.Start)).TotalMinutes))
            .OrderByDescending(x => x.Minutes)
            .ThenBy(x => x.Outcome.Night.StartDate)
            .FirstOrDefault();
        var mostGames = played
            .OrderByDescending(x => x.Night.Sessions.Count)
            .ThenBy(x => x.Night.StartDate)
            .FirstOrDefault();
        var biggestTable = played
            .Where(x => x.Night.AttendeeIds.Count > 0)
            .OrderByDescending(x => x.Night.AttendeeIds.Count)
            .ThenBy(x => x.Night.StartDate)
            .FirstOrDefault();
        var latest = played
            .SelectMany(x => x.Night.Sessions.Select(s => (Outcome: x, Session: s)))
            .OrderByDescending(x => _dateTimeProvider.ConvertToLocalTime(x.Session.End) - _dateTimeProvider.ConvertToLocalTime(x.Outcome.Night.StartDate).Date)
            .ThenBy(x => x.Outcome.Night.StartDate)
            .FirstOrDefault();

        return new GameNightRecordsDto
        {
            LongestNight = longest.Outcome == null ? null : ToRecord(longest.Outcome, longest.Minutes),
            MostGames = mostGames == null ? null : ToRecord(mostGames, mostGames.Night.Sessions.Count, TopGame(mostGames.Night)),
            BiggestTable = biggestTable == null ? null : ToRecord(biggestTable, biggestTable.Night.AttendeeIds.Count),
            LatestFinish = latest.Outcome == null
                ? null
                : ToRecord(latest.Outcome, (latest.Session.End - latest.Session.Start).TotalMinutes, latest.Session.GameId, latest.Session.End)
        };
    }

    private static int? TopGame(GameNightStatisticsNight night)
    {
        var counts = night.Sessions
            .GroupBy(x => x.GameId)
            .Select(g => (GameId: g.Key, Count: g.Count()))
            .OrderByDescending(x => x.Count)
            .ToList();
        if (counts[0].Count < 2 || (counts.Count > 1 && counts[1].Count == counts[0].Count))
        {
            return null;
        }

        return counts[0].GameId;
    }

    private static GameNightRecordDto ToRecord(NightOutcome outcome, double value, int? gameId = null, DateTime? at = null)
    {
        return new GameNightRecordDto
        {
            GameNightId = outcome.Night.Id,
            Title = outcome.Night.Title,
            StartDate = outcome.Night.StartDate,
            GamesPlayed = outcome.Night.Sessions.Count,
            Value = value,
            GameId = gameId,
            At = at
        };
    }

    private static GameNightReferenceDto ToReference(GameNightStatisticsNight night)
    {
        return new GameNightReferenceDto { GameNightId = night.Id, Title = night.Title, StartDate = night.StartDate };
    }

    private sealed record NightOutcome(GameNightStatisticsNight Night, NightWinners Winners);
}
