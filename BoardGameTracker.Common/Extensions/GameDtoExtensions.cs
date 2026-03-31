using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class GameDtoExtensions
{
    public static GameDto ToDto(this Game game)
    {
        return new GameDto
        {
            Id = game.Id,
            Title = game.Title,
            YearPublished = game.YearPublished,
            Description = game.Description,
            Image = game.Image,
            MinPlayers = game.PlayerCount?.Min,
            MaxPlayers = game.PlayerCount?.Max,
            MinPlayTime = game.PlayTime?.MinMinutes,
            MaxPlayTime = game.PlayTime?.MaxMinutes,
            MinAge = game.MinAge,
            Rating = game.Rating?.Value,
            isLoaned = game.IsLoaned,
            Weight = game.Weight?.Value,
            BggId = game.BggId,
            State = game.State,
            HasScoring = game.HasScoring,
            BuyingPrice = game.BuyingPrice?.Amount,
            SoldPrice = game.SoldPrice?.Amount,
            AdditionDate = game.AdditionDate,
            Expansions = game.Expansions?.Select(e => e.ToDto()).ToList(),
            Categories = game.Categories?.Select(c => new GameLinkDto { Id = c.Id, Name = c.Name }).ToList(),
            Mechanics = game.Mechanics?.Select(m => new GameLinkDto { Id = m.Id, Name = m.Name }).ToList(),
            People = game.People?.Select(p => new GamePersonDto { Id = p.Id, Name = p.Name, Type = p.Type }).ToList()
        };
    }

    public static List<GameDto> ToListDto(this IEnumerable<Game> games)
    {
        return games.Select(g => g.ToDto()).ToList();
    }
}