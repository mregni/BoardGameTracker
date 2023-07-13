using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameRepository : IGameRepository
{
    private readonly MainContext _context;

    public GameRepository(MainContext context)
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
}