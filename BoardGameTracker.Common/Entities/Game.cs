using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Game : HasId
{
    
    public string Title { get; set; }
    public string Description { get; set; }
    public int? YearPublished { get; set; }
    public string Image { get; set; }
    public int? MinPlayers { get; set; }
    public int? MaxPlayers { get; set; }
    public int? MinPlayTime { get; set; }
    public int? MaxPlayTime { get; set; }
    public int? MinAge { get; set; }
    public double? Rating { get; set; }
    public double? Weight { get; set; }
    public int? BggId { get; set; }
    public GameType Type { get; set; }
    public GameState State { get; set; }
    
    public Game BaseGame { get; set; }
    public int? BaseGameId { get; set; }
    public ICollection<Game> Expansions { get; set; }
    public ICollection<GameAccessory> Accessories { get; set; }
    public ICollection<GameCategory> Categories { get; set; }
    public ICollection<GameMechanic> Mechanics { get; set; }
    public ICollection<Person> People { get; set; }
}