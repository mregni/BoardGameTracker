using BoardGameTracker.Common.Entities;
using BoardGameTracker.Core.Common;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using BoardGameTracker.Core.Sessions.Specifications;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameSessionRepository : EfReadRepository<Session>, IGameSessionRepository
{
    private readonly IDateTimeProvider _dateTimeProvider;

    public GameSessionRepository(MainDbContext context, IDateTimeProvider dateTimeProvider) : base(context)
    {
        _dateTimeProvider = dateTimeProvider;
    }

    public Task<List<Session>> GetSessions(int gameId, int skip, int? take)
    {
        return ListAsync(new SessionsByGamePagedSpec(gameId, skip, take));
    }

    public Task<List<Session>> GetSessions(int gameId, int dayCount)
    {
        var cutoff = _dateTimeProvider.UtcNow.AddDays(dayCount);
        return ListAsync(new SessionsByGameSinceSpec(gameId, cutoff));
    }

    public Task<List<Session>> GetSessionsByGameId(int gameId, int? count)
    {
        return ListAsync(new SessionsByGameSpec(gameId, count));
    }

    public Task<List<Session>> GetSessionsByPlayerId(int playerId, int? count)
    {
        return ListAsync(new SessionsByPlayerRecentFirstSpec(playerId, count));
    }

    public Task<int> GetPlayCount(int gameId)
    {
        return CountAsync(new SessionsByGameSpec(gameId));
    }

    public async Task<double> GetTotalPlayedTime(int gameId)
    {
        var totalDurationInMinutes = await Context.Sessions
            .AsNoTracking()
            .Where(x => x.GameId == gameId)
            .SumAsync(session => (session.End - session.Start).TotalMinutes);

        return totalDurationInMinutes;
    }

    public Task<DateTime?> GetLastPlayedDateTime(int gameId)
    {
        return FirstOrDefaultAsync(new LastPlayedDateSpec(gameId));
    }

    public Task<int?> GetShortestPlay(int gameId)
    {
        return FirstOrDefaultAsync(new ShortestPlayIdSpec(gameId));
    }

    public Task<int?> GetLongestPlay(int gameId)
    {
        return FirstOrDefaultAsync(new LongestPlayIdSpec(gameId));
    }
}
