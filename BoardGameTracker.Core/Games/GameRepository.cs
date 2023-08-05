using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameRepository : IGameRepository
{
    private readonly MainDbContext _dbContext;

    public GameRepository(MainDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories)
    {
        await _dbContext.GameCategories.AddRangeIfNotExists(categories);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics)
    {
        await _dbContext.GameMechanics.AddRangeIfNotExists(mechanics);
        await _dbContext.SaveChangesAsync();
    }

    public async Task AddPeopleIfNotExists(IEnumerable<Person> people)
    {
        await _dbContext.People.AddRangeIfNotExists(people);
        await _dbContext.SaveChangesAsync();
    }

    public async Task<Game> InsertGame(Game game)
    {
        var catIds = game.Categories.Select(x => x.Id);
        game.Categories = await _dbContext.GameCategories.Where(x => catIds.Contains(x.Id)).ToListAsync();

        var mecIds = game.Mechanics.Select(x => x.Id);
        game.Mechanics = await _dbContext.GameMechanics.Where(x => mecIds.Contains(x.Id)).ToListAsync();
            
        var peopleIds = game.People.Select(x => x.Id);
        game.People = await _dbContext.People.Where(x => peopleIds.Contains(x.Id)).ToListAsync();

        await _dbContext.Games.AddAsync(game);
        await _dbContext.SaveChangesAsync();

        return game;
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _dbContext.Games
            .SingleOrDefaultAsync(x => x.BggId == bggId);
    }

    public Task<List<Game>> GetGamesOverviewList()
    {
        return _dbContext.Games
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    public Task<Game?> GetGameById(int id, bool includePlays)
    {
        var query = _dbContext.Games
            .Include(x => x.Accessories)
            .Include(x => x.Categories)
            .Include(x => x.Expansions)
            .Include(x => x.Mechanics)
            .Include(x => x.People)
            .AsQueryable();
        
         if (includePlays)
        {
            query = query
                .Include(x => x.Plays)
                .ThenInclude(x => x.Sessions)
                .Include(x => x.Plays)
                .ThenInclude(x => x.Players);
        }
        return query.SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task DeleteGame(Game game)
    {
        _dbContext.Remove(game);
        return _dbContext.SaveChangesAsync();
    }
}