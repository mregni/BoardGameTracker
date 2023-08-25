using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameRepository : IGameRepository
{
    private readonly MainDbContext _context;

    public GameRepository(MainDbContext context)
    {
        _context = context;
    }

    public async Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories)
    {
        await _context.GameCategories.AddRangeIfNotExists(categories);
        await _context.SaveChangesAsync();
    }

    public async Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics)
    {
        await _context.GameMechanics.AddRangeIfNotExists(mechanics);
        await _context.SaveChangesAsync();
    }

    public async Task AddPeopleIfNotExists(IEnumerable<Person> people)
    {
        await _context.People.AddRangeIfNotExists(people);
        await _context.SaveChangesAsync();
    }

    public async Task<Game> InsertGame(Game game)
    {
        var catIds = game.Categories.Select(x => x.Id);
        game.Categories = await _context.GameCategories.Where(x => catIds.Contains(x.Id)).ToListAsync();

        var mecIds = game.Mechanics.Select(x => x.Id);
        game.Mechanics = await _context.GameMechanics.Where(x => mecIds.Contains(x.Id)).ToListAsync();
            
        var peopleIds = game.People.Select(x => x.Id);
        game.People = await _context.People.Where(x => peopleIds.Contains(x.Id)).ToListAsync();

        await _context.Games.AddAsync(game);
        await _context.SaveChangesAsync();

        return game;
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _context.Games
            .SingleOrDefaultAsync(x => x.BggId == bggId);
    }

    public Task<List<Game>> GetGamesOverviewList()
    {
        return _context.Games
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    public Task<Game?> GetGameById(int id)
    {
        return _context.Games
            .Include(x => x.Accessories)
            .Include(x => x.Categories)
            .Include(x => x.Expansions)
            .Include(x => x.Mechanics)
            .Include(x => x.People)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task DeleteGame(Game game)
    {
        _context.Remove(game);
        return _context.SaveChangesAsync();
    }

    public Task<List<Play>> GetPlays(int id)
    {
        return _context.Plays
            .Include(x => x.Location)
            .Include(x => x.Players)
            .Where(x => x.GameId == id)
            .OrderByDescending(x => x.Start)
            .ToListAsync();
    }

    public Task<int> GetPlayCount(int id)
    {
        return _context.Plays
            .Where(x => x.GameId == id)
            .CountAsync();
    }

    public async Task<TimeSpan> GetTotalPlayedTime(int id)
    {
        var totalDurationInMinutes = await _context.Plays
            .Where(x => x.GameId == id)
            .SumAsync(session => (session.End - session.Start).TotalMinutes);

        return TimeSpan.FromMinutes(totalDurationInMinutes);
    }

    public async Task<double?> GetPricePerPlay(int id)
    {
        var game = await _context.Games
            .Include(x => x.Plays)
            .Where(x => x.Id == id)
            .SingleAsync();
        
        if (game.Plays.Count == 0 || !game.BuyingPrice.HasValue)
        {
            return null;
        }

        return game.BuyingPrice.Value / game.Plays.Count;
    }

    public Task<int> GetUniquePlayerCount(int id)
    {
        return _context.Plays
            .Include(x => x.Players)
            .Where(x => x.GameId == id)
            .SelectMany(x => x.Players)
            .GroupBy(x => x.PlayerId)
            .CountAsync();
    }

    public Task<double?> GetHighestScore(int id)
    {
        return _context.Plays
            .Include(x => x.Players)
            .Where(x => x.GameId == id)
            .SelectMany(x => x.Players)
            .MaxAsync(x => x.Score);
    }

    public async Task<Player?> GetMostWins(int id)
    {
        var playerId = await _context.Plays
            .Include(x => x.Players)
            .Where(x => x.GameId == id)
            .SelectMany(x => x.Players)
            .Where(x => x.Won)
            .GroupBy(x => x.PlayerId)
            .OrderByDescending(x => x.Count())
            .Select(x => x.Key)
            .FirstOrDefaultAsync();

        if (!playerId.HasValue)
        {
            return null;
        }
        return await _context.Players.FirstAsync(x => x.Id == playerId);
    }

    public Task<double?> GetAverageScore(int id)
    {
        return _context.Plays
            .Include(x => x.Players)
            .Where(x => x.GameId == id)
            .SelectMany(x => x.Players)
            .AverageAsync(x => x.Score);
    }
}