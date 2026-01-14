using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.DTOs;

public class PlayerDto
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Image { get; set; }
    public List<BadgeDto>? Badges { get; set; }
}

public static class PlayerDtoExtensions
{
    public static PlayerDto ToDto(this Player player)
    {
        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            Image = player.Image,
            Badges = player.Badges?.Select(b => b.ToDto()).ToList()
        };
    }

    public static List<PlayerDto> ToListDto(this IEnumerable<Player> players)
    {
        return players.Select(p => p.ToDto()).ToList();
    }

    public static Player ToEntity(this PlayerDto dto)
    {
        return new Player(dto.Name, dto.Image);
    }
}
