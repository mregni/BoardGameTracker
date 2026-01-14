using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class GameDto
{
    public int Id { get; set; }
    public string Title { get; set; } = string.Empty;
    public int? YearPublished { get; set; }
    public string Description { get; set; } = string.Empty;
    public string? Image { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public bool isLoaned { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int? BggId { get; set; }
    public GameState State { get; set; }
    public bool HasScoring { get; set; }
    public decimal? BuyingPrice { get; set; }
    public decimal? SoldPrice { get; set; }
    public DateTime? AdditionDate { get; set; }
    public List<ExpansionDto>? Expansions { get; set; }
    public List<GameLinkDto>? Categories { get; set; }
    public List<GameLinkDto>? Mechanics { get; set; }
    public List<GamePersonDto>? People { get; set; }
}

public class GameLinkDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
}

public class GamePersonDto : GameLinkDto
{
    public PersonType Type { get; set; }
}

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

    public static Game ToEntity(this GameDto dto)
    {
        var game = new Game(dto.Title, dto.HasScoring, dto.State)
        {
            Id = dto.Id
        };

        game.UpdateYearPublished(dto.YearPublished);
        game.UpdateDescription(dto.Description);
        game.UpdateImage(dto.Image);
        game.UpdatePlayerCount(dto.MinPlayers, dto.MaxPlayers);
        game.UpdatePlayTime(dto.MinPlayTime, dto.MaxPlayTime);
        game.UpdateMinAge(dto.MinAge);
        game.UpdateRating(dto.Rating);
        game.UpdateWeight(dto.Weight);
        game.UpdateBggId(dto.BggId);
        game.UpdateBuyingPrice(dto.BuyingPrice);
        game.UpdateSoldPrice(dto.SoldPrice);
        game.UpdateAdditionDate(dto.AdditionDate);

        return game;
    }
}
