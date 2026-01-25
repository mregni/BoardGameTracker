using System.ComponentModel.DataAnnotations;

namespace BoardGameTracker.Common.DTOs.Commands;

public class CreateLocationCommand
{
    public required string Name { get; set; }
}

public class UpdateLocationCommand : CreateLocationCommand
{
    public int Id { get; set; }
}
