namespace BoardGameTracker.Common.ViewModels;

public class ExpansionViewModel
{
    public int Id { get; set; }
    public string Title { get; set; }
    public int BggId { get; set; }
    public int? GameId { get; set; }
}