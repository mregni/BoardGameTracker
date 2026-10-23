using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Auth;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Common.Extensions;
using BoardGameTracker.Core.Auth.Interfaces;
using BoardGameTracker.Core.Datastore;
using BoardGameTracker.Core.Players.Interfaces;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Auth;

public class PlayerLinkPromptService : IPlayerLinkPromptService
{
    private const int MaxPlayerNameLength = 200;

    private readonly UserManager<ApplicationUser> _userManager;
    private readonly IUserAdminService _userAdminService;
    private readonly IPlayerService _playerService;
    private readonly MainDbContext _context;
    private readonly ILogger<PlayerLinkPromptService> _logger;

    public PlayerLinkPromptService(
        UserManager<ApplicationUser> userManager,
        IUserAdminService userAdminService,
        IPlayerService playerService,
        MainDbContext context,
        ILogger<PlayerLinkPromptService> logger)
    {
        _userManager = userManager;
        _userAdminService = userAdminService;
        _playerService = playerService;
        _context = context;
        _logger = logger;
    }

    public async Task<PlayerLinkSuggestionsResponse> GetSuggestionsAsync(string userId)
    {
        var user = await FindUserAsync(userId);
        var roles = await _userManager.GetRolesAsync(user);

        var linkedPlayerIds = _context.Users
            .Where(u => u.PlayerId != null)
            .Select(u => u.PlayerId!.Value);
        var candidates = await _context.Players
            .AsNoTracking()
            .Where(p => !linkedPlayerIds.Contains(p.Id))
            .Select(p => new PlayerLinkCandidate(p.Id, p.Name, p.Image, p.Email))
            .ToListAsync();
        candidates.Sort((left, right) => StringComparer.InvariantCultureIgnoreCase.Compare(left.Name, right.Name));

        var suggestions = PlayerLinkMatcher.Suggest(candidates, user.Email, user.DisplayName, user.UserName);
        var players = candidates.Select(p => new PlayerLinkCandidateDto(p.Id, p.Name, p.Image)).ToList();
        return new PlayerLinkSuggestionsResponse(suggestions, players, CanCreatePlayer(roles));
    }

    public async Task<ProfileResponse> LinkAsync(string userId, int playerId)
    {
        var user = await FindUserAsync(userId);
        if (!await _context.Players.AnyAsync(p => p.Id == playerId))
        {
            throw new EntityNotFoundException(nameof(Player), playerId);
        }

        if (await _context.Users.AnyAsync(u => u.PlayerId == playerId && u.Id != userId))
        {
            throw new ConflictException(Constants.Errors.PlayerAlreadyLinked);
        }

        await _userAdminService.LinkPlayerAccountAsync(playerId, userId);
        _logger.LogInformation("User {UserId} linked their account to player {PlayerId}", userId, playerId);
        return await ToProfileAsync(user);
    }

    public async Task<ProfileResponse> CreatePlayerAsync(string userId)
    {
        var user = await FindUserAsync(userId);
        if (!CanCreatePlayer(await _userManager.GetRolesAsync(user)))
        {
            _logger.LogWarning("User {UserId} may not create a player for their account", userId);
            throw new ForbiddenException(Constants.Errors.PlayerCreateNotAllowed);
        }

        var existing = await _context.Players
            .AsNoTracking()
            .Select(p => new PlayerIdentity(p.Name, p.Email))
            .ToListAsync();
        var player = await _playerService.Create(new CreatePlayerCommand
        {
            Name = ChooseName(user, existing),
            Email = ChooseEmail(user.Email, existing)
        });

        await _userAdminService.LinkPlayerAccountAsync(player.Id, userId);
        _logger.LogInformation("User {UserId} created player {PlayerId} for their account", userId, player.Id);
        return await ToProfileAsync(user);
    }

    public async Task<ProfileResponse> SkipAsync(string userId)
    {
        var user = await FindUserAsync(userId);
        user.SkipPlayerLink();
        var result = await _userManager.UpdateAsync(user);
        if (!result.Succeeded)
        {
            throw new DomainException(Constants.Errors.PlayerLinkUpdateFailed);
        }

        _logger.LogInformation("User {UserId} skipped linking their account to a player", userId);
        return await ToProfileAsync(user);
    }

    private static bool CanCreatePlayer(IEnumerable<string> roles) =>
        roles.Any(r => r is Constants.AuthRoles.Admin or Constants.AuthRoles.User);

    private async Task<ApplicationUser> FindUserAsync(string userId) =>
        await _userManager.FindByIdAsync(userId)
        ?? throw new EntityNotFoundException(nameof(ApplicationUser), userId);

    private async Task<ProfileResponse> ToProfileAsync(ApplicationUser user)
    {
        var roles = await _userManager.GetRolesAsync(user);
        return user.ToProfileDto(roles);
    }

    private static string ChooseName(ApplicationUser user, List<PlayerIdentity> existing)
    {
        var options = new[] { user.DisplayName, user.UserName }
            .Select(n => n?.Trim())
            .Where(n => !string.IsNullOrEmpty(n))
            .Select(n => n!.Length > MaxPlayerNameLength ? n[..MaxPlayerNameLength].TrimEnd() : n!)
            .ToList();

        return options.FirstOrDefault(o => !existing.Exists(p => string.Equals(p.Name.Trim(), o, StringComparison.OrdinalIgnoreCase)))
            ?? options.FirstOrDefault()
            ?? string.Empty;
    }

    private static string? ChooseEmail(string? email, List<PlayerIdentity> existing)
    {
        var trimmed = email?.Trim();
        if (string.IsNullOrEmpty(trimmed) || trimmed.EndsWith(OidcService.PlaceholderEmailDomain, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var taken = existing.Exists(p => string.Equals(p.Email?.Trim(), trimmed, StringComparison.OrdinalIgnoreCase));
        return taken ? null : trimmed;
    }

    private sealed record PlayerIdentity(string Name, string? Email);
}
