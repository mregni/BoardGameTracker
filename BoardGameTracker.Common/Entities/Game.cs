using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.Entities;

public class Game : BaseGame
{
    public bool HasScoring { get; private set; }
    public ICollection<Expansion> Expansions { get; private set; }
    public ICollection<GameAccessory> Accessories { get; private set; }
    public ICollection<GameCategory> Categories { get; private set; }
    public ICollection<GameMechanic> Mechanics { get; private set; }
    public ICollection<Person> People { get; private set; }

    public Game(string title, bool hasScoring = false, GameState state = GameState.Owned) : base(title, state)
    {
        HasScoring = hasScoring;
        Expansions = new List<Expansion>();
        Accessories = new List<GameAccessory>();
        Categories = new List<GameCategory>();
        Mechanics = new List<GameMechanic>();
        People = new List<Person>();
    }

    public void UpdateHasScoring(bool hasScoring)
    {
        HasScoring = hasScoring;
    }

    public void AddExpansion(Expansion expansion)
    {
        Guard.Against.Null(expansion);
        if (!Expansions.Any(e => e.BggId == expansion.BggId))
        {
            Expansions.Add(expansion);
        }
    }

    public void RemoveExpansion(int expansionBggId)
    {
        var expansion = Expansions.FirstOrDefault(e => e.BggId == expansionBggId);
        if (expansion != null)
        {
            Expansions.Remove(expansion);
        }
    }

    public Loan LoanToPlayer(int playerId, DateTime loanDate, DateTime? dueDate = null)
    {
        var loan = new Loan(Id, playerId, loanDate);
        Loans.Add(loan);
        return loan;
    }
}