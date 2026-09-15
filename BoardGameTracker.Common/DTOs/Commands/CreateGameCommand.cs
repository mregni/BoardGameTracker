using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateGameCommand : IValidatableObject
{
    [Required]
    [StringLength(500)]
    public required string Title { get; set; }

    [Range(0, 9999)]
    public int? YearPublished { get; set; }

    public string? Image { get; set; }

    [StringLength(2048)]
    public string? ShopUrl { get; set; }

    [StringLength(100)]
    public string? ChangeDetectionWatchId { get; set; }

    [StringLength(10)]
    public string? Language { get; set; }

    public string? Description { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinPlayers { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxPlayers { get; set; }

    [Range(1, int.MaxValue)]
    public int? MinPlayTime { get; set; }

    [Range(1, int.MaxValue)]
    public int? MaxPlayTime { get; set; }

    [Range(0, 120)]
    public int? MinAge { get; set; }

    [Range(1, int.MaxValue)]
    public int? BggId { get; set; }

    public GameState State { get; set; }
    public bool HasScoring { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? BuyingPrice { get; set; }

    public DateTime? AdditionDate { get; set; }

    public IEnumerable<ValidationResult> Validate(ValidationContext validationContext)
    {
        foreach (var result in ValidateRange(MinPlayers, MaxPlayers, nameof(MinPlayers), nameof(MaxPlayers), Constants.Errors.PlayerRangeIncomplete))
        {
            yield return result;
        }

        foreach (var result in ValidateRange(MinPlayTime, MaxPlayTime, nameof(MinPlayTime), nameof(MaxPlayTime), Constants.Errors.PlayTimeRangeIncomplete))
        {
            yield return result;
        }
    }

    private static IEnumerable<ValidationResult> ValidateRange(int? min, int? max, string minName, string maxName, string incompleteError)
    {
        if (min.HasValue != max.HasValue)
        {
            yield return new ValidationResult(incompleteError, [minName, maxName]);
        }
        else if (min > max)
        {
            yield return new ValidationResult(Constants.Errors.RangeMinAboveMax, [minName, maxName]);
        }
    }
}

public class UpdateGameCommand : CreateGameCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }

    [Range(0, 10)]
    public double? Rating { get; set; }

    [Range(0, 5)]
    public double? Weight { get; set; }

    [Range(0, double.MaxValue)]
    public decimal? SoldPrice { get; set; }
}
