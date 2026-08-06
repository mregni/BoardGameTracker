namespace BoardGameTracker.Common.DTOs.Commands;

public class CreatePlayerCommand
{
    public required string Name { get; set; }
    public string? Image { get; set; }
    public string? Email { get; set; }
}

public class UpdatePlayerCommand : CreatePlayerCommand
{
    public int Id { get; set; }
}
