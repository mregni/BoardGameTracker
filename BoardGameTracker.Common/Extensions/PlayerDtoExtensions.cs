using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Common.Extensions;

public static class PlayerDtoExtensions
{
    public static PlayerDto? ToDto(this Player? player, PlayerLinkedAccountDto? linkedAccount = null)
    {
        if (player == null)
        {
            return null;
        }

        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            Image = player.Image,
            ImageSource = player.ImageSource,
            Email = player.Email,
            Badges = player.PlayerBadges?.Select(b => b.ToDto()).ToList(),
            LinkedAccount = linkedAccount
        };
    }

    public static PlayerDto? ToPublicDto(this Player? player)
    {
        if (player == null)
        {
            return null;
        }

        return new PlayerDto
        {
            Id = player.Id,
            Name = player.Name,
            Image = player.Image
        };
    }

    public static List<PlayerDto> ToListDto(this IEnumerable<Player> players)
    {
        return players
            .Select(p => p.ToDto())
            .OfType<PlayerDto>()
            .ToList();
    }
}
