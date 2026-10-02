using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateGameNightCommand
{
    [Required]
    [StringLength(200)]
    public required string Title { get; set; }

    public string Notes { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }

    [Range(1, int.MaxValue)]
    public int HostId { get; set; }

    [Range(1, int.MaxValue)]
    public int LocationId { get; set; }
    public List<int> SuggestedGameIds { get; set; } = [];
    public List<int> InvitedPlayerIds { get; set; } = [];
}

public class UpdateGameNightCommand : CreateGameNightCommand
{
    [Range(1, int.MaxValue)]
    public int Id { get; set; }
}

public class UpdateRsvpCommand
{
    [Range(1, int.MaxValue)]
    public int? Id { get; set; }

    [Range(1, int.MaxValue)]
    public int? GameNightId { get; set; }

    [Range(1, int.MaxValue)]
    public int? PlayerId { get; set; }
    public GameNightRsvpState State { get; set; }
}
