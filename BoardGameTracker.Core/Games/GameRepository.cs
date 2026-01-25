using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Games.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Games;

public class GameRepository : CrudHelper<Game>, IGameRepository
{
    private readonly MainDbContext _context;

    public GameRepository(MainDbContext context): base(context)
    {
        _context = context;
    }

    public async Task AddGameCategoriesIfNotExists(IEnumerable<GameCategory> categories)
    {
        await _context.GameCategories.AddRangeIfNotExists(categories);
    }

    public async Task AddGameMechanicsIfNotExists(IEnumerable<GameMechanic> mechanics)
    {
        await _context.GameMechanics.AddRangeIfNotExists(mechanics);
    }

    public async Task AddPeopleIfNotExists(IEnumerable<Person> people)
    {
        await _context.People.AddRangeIfNotExists(people);
    }

    public override async Task<Game> CreateAsync(Game entity)
    {
        await _context.Games.AddAsync(entity);
        return entity;
    }

    public Task<Game?> GetGameByBggId(int bggId)
    {
        return _context.Games
            .SingleOrDefaultAsync(x => x.BggId == bggId);
    }

    public Task<List<Game>> GetGamesOverviewList()
    {
        return _context.Games
            .AsNoTracking()
            .AsSplitQuery()
            .Include(x => x.Expansions)
            .Include(x => x.Categories)
            .OrderBy(x => x.Title)
            .ToListAsync();
    }

    public override Task<Game?> GetByIdAsync(int id)
    {
        return _context.Games
            .Include(x => x.Accessories)
            .Include(x => x.Categories)
            .Include(x => x.Expansions)
            .Include(x => x.Mechanics)
            .Include(x => x.People)
            .SingleOrDefaultAsync(x => x.Id == id);
    }

    public Task<List<Expansion>> GetExpansions(List<int> expansionIds)
    {
        return _context.Expansions
            .Where(x => expansionIds.Contains(x.Id))
            .ToListAsync();
    }

    public Task<int> GetTotalExpansionCount()
    {
        return _context.Expansions.CountAsync();
    }

    public Task<int> CountAsync()
    {
        return _context.Games.CountAsync();
    }

    public async Task DeleteExpansion(int gameId, int expansionId)
    {
        var game = await _context.Games
            .Include(x => x.Expansions)
            .SingleOrDefaultAsync(x => x.Id == gameId);

        if (game == null)
        {
            return;
        }

        var expansion = game.Expansions.FirstOrDefault(e => e.Id == expansionId);
        if (expansion != null)
        {
            game.RemoveExpansion(expansion.BggId);
        }
    }

    public Task<List<Game>> GetRecentlyAddedGames(int count)
    {
        return _context.Games
            .AsNoTracking()
            .Where(x => x.AdditionDate != null)
            .OrderByDescending(x => x.AdditionDate)
            .Take(count)
            .ToListAsync();
    }

    public Task<List<Game>> GetGamesWithNoRecentSessions(DateTime cutoffDate)
    {
        return _context.Games
            .AsNoTracking()
            .Where(g => !_context.Sessions.Any(s => s.GameId == g.Id && s.Start >= cutoffDate))
            .OrderBy(g => g.Title)
            .ToListAsync();
    }

    public Task<int> CountGamesWithNoRecentSessions(DateTime cutoffDate)
    {
        return _context.Games
            .Where(g => !_context.Sessions.Any(s => s.GameId == g.Id && s.Start >= cutoffDate))
            .CountAsync();
    }

    public override async Task<Game> UpdateAsync(Game entity)
    {
        var dbGame = await _context.Games
            .Include(x => x.Expansions)
            .SingleOrDefaultAsync(x => x.Id == entity.Id);
        if (dbGame != null)
        {
            dbGame.UpdateHasScoring(entity.HasScoring);
            dbGame.UpdateDescription(entity.Description);
            dbGame.UpdateImage(entity.Image);
            dbGame.UpdateRating(entity.Rating?.Value);
            dbGame.UpdateState(entity.State);
            dbGame.UpdateTitle(entity.Title);
            dbGame.UpdateWeight(entity.Weight?.Value);
            dbGame.UpdateBggId(entity.BggId);
            dbGame.UpdateBuyingPrice(entity.BuyingPrice?.Amount);
            dbGame.UpdatePlayerCount(entity.PlayerCount?.Min, entity.PlayerCount?.Max);
            dbGame.UpdateMinAge(entity.MinAge);
            dbGame.UpdateSoldPrice(entity.SoldPrice?.Amount);
            dbGame.UpdateYearPublished(entity.YearPublished);
            dbGame.UpdatePlayTime(entity.PlayTime?.MinMinutes, entity.PlayTime?.MaxMinutes);
            dbGame.UpdateAdditionDate(entity.AdditionDate);

            var expansionsToRemove = dbGame.Expansions.Where(e => entity.Expansions.All(ne => ne.Id != e.Id)).ToList();
            foreach (var expansion in expansionsToRemove)
            {
                dbGame.RemoveExpansion(expansion.BggId);
            }

            var expansionsToAdd = entity.Expansions.Where(e => dbGame.Expansions.All(de => de.Id != e.Id)).ToList();
            foreach (var expansion in expansionsToAdd)
            {
                dbGame.AddExpansion(expansion);
            }
        }

        return entity;
    }
    
}