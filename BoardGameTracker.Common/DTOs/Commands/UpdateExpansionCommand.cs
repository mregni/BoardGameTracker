using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class UpdateExpansionCommand
{
    public GameState State { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BuyingPrice { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SoldPrice { get; set; }

    public DateTime? AdditionDate { get; set; }
}
