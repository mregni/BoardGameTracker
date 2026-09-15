using Ardalis.GuardClauses;
using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;

namespace BoardGameTracker.Common.Entities;

public class Game : BaseGame
{
    public bool HasScoring { get; private set; }
    public ICollection<Expansion> Expansions { get; private set; }
    public ICollection<GameAccessory> Accessories { get; private set; }
    public ICollection<GameCategory> Categories { get; private set; }
    public ICollection<GameMechanic> Mechanics { get; private set; }
    public ICollection<Person> People { get; private set; }
    public ICollection<Manual> Manuals { get; private set; }

    public Game(string title, bool hasScoring = false, GameState state = GameState.Owned) : base(title, state)
    {
        HasScoring = hasScoring;
        Expansions = new List<Expansion>();
        Accessories = new List<GameAccessory>();
        Categories = new List<GameCategory>();
        Mechanics = new List<GameMechanic>();
        People = new List<Person>();
        Manuals = new List<Manual>();
    }

    public void UpdateHasScoring(bool hasScoring)
    {
        HasScoring = hasScoring;
    }

    public void AddCategory(GameCategory category)
    {
        Guard.Against.Null(category);
        if (Categories.All(c => c.Name != category.Name))
        {
            Categories.Add(category);
        }
    }

    public void AddMechanic(GameMechanic mechanic)
    {
        Guard.Against.Null(mechanic);
        if (Mechanics.All(m => m.Name != mechanic.Name))
        {
            Mechanics.Add(mechanic);
        }
    }

    public void AddPerson(Person person)
    {
        Guard.Against.Null(person);
        if (People.All(p => p.Name != person.Name || p.Type != person.Type))
        {
            People.Add(person);
        }
    }

    public void AddExpansion(Expansion expansion)
    {
        Guard.Against.Null(expansion);
        if (!Expansions.Any(e => e.Matches(expansion)))
        {
            Expansions.Add(expansion);
        }
    }

    public void RemoveExpansion(Expansion expansion)
    {
        Guard.Against.Null(expansion);
        Expansions.Remove(expansion);
    }

    public Loan LoanToPlayer(int playerId, DateTime loanDate)
    {
        if (IsLoanedOn(loanDate))
        {
            throw new DomainException(Constants.Errors.GameAlreadyOnLoan);
        }

        var loan = new Loan(Id, playerId, loanDate);
        Loans.Add(loan);
        return loan;
    }
}