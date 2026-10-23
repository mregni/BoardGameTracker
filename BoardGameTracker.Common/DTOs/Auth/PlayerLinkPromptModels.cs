using System.ComponentModel.DataAnnotations;
using BoardGameTracker.Common.Enums;

namespace BoardGameTracker.Common.DTOs.Auth;

public record PlayerLinkCandidateDto(int Id, string Name, string? Image);

public record PlayerLinkSuggestionDto(int Id, string Name, string? Image, PlayerLinkMatchReason Reason);

public record PlayerLinkSuggestionsResponse(
    List<PlayerLinkSuggestionDto> Suggestions,
    List<PlayerLinkCandidateDto> Players,
    bool CanCreatePlayer);

public class LinkOwnPlayerRequest
{
    [Range(1, int.MaxValue)]
    public int PlayerId { get; set; }
}
