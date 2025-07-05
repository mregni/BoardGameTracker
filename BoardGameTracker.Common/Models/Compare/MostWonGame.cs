namespace BoardGameTracker.Common.Models.Compare;

public class MostWonGame
{
    public int? GameId { get; set; }
    public int Count { get; set; }

    public MostWonGame()
    {
        Count = 0;
    }
}