using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

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
}
