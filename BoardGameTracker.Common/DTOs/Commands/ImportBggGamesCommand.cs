using BoardGameTracker.Common.Models;

namespace BoardGameTracker.Common.DTOs.Commands;

public class ImportBggGamesCommand
{
    public List<ImportGame> Games { get; set; } = [];
}
