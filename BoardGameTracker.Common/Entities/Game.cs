using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Common.Entities;

public class Game : BaseGame
{
    public bool HasScoring { get; set; }
    public ICollection<Expansion> Expansions { get; set; } = null!;
    public ICollection<GameAccessory> Accessories { get; set; } = null!;
    public ICollection<GameCategory> Categories { get; set; } = null!;
    public ICollection<GameMechanic> Mechanics { get; set; } = null!;
    public ICollection<Person> People { get; set; } = null!;
    public ICollection<Loan> Loans { get; set; } = null!;

    public Game()
    {
 
    }
}