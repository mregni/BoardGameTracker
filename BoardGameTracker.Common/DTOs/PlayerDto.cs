using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs;

public class PlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public PlayerImageSource ImageSource { get; set; }
    public string? Email { get; set; }
    public List<BadgeDto>? Badges { get; set; }
    public PlayerLinkedAccountDto? LinkedAccount { get; set; }
}

public class PlayerLinkedAccountDto
{
    public string Id { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? ProfileImage { get; set; }
}
