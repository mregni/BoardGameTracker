using BoardGameTracker.Common.DTOs;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Entities.Auth;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Core.Auth.Specifications;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Images.Interfaces;
using BoardGameTracker.Core.Players.Interfaces;
using BoardGameTracker.Core.Players.Specifications;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Players;

public class PlayerAccountImageService : IPlayerAccountImageService
{
    private readonly IPlayerRepository _playerRepository;
    private readonly IReadRepository<ApplicationUser> _userRepository;
    private readonly IImageService _imageService;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<PlayerAccountImageService> _logger;

    public PlayerAccountImageService(
        IPlayerRepository playerRepository,
        IReadRepository<ApplicationUser> userRepository,
        IImageService imageService,
        IUnitOfWork unitOfWork,
        ILogger<PlayerAccountImageService> logger)
    {
        _playerRepository = playerRepository;
        _userRepository = userRepository;
        _imageService = imageService;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public async Task<bool> ApplyAccountImageAsync(ApplicationUser user)
    {
        if (user.PlayerId is not { } playerId)
        {
            return false;
        }

        var player = await _playerRepository.SingleOrDefaultAsync(new PlayerByIdForUpdateSpec(playerId));
        if (player == null || !player.FollowsAccountImage)
        {
            return false;
        }

        return await ReplaceAsync(player, user.ProfileImage);
    }

    public async Task ReleaseAsync(int? playerId)
    {
        if (playerId is not { } id)
        {
            return;
        }

        var player = await _playerRepository.SingleOrDefaultAsync(new PlayerByIdForUpdateSpec(id));
        if (player == null || player.ImageSource != PlayerImageSource.Account)
        {
            return;
        }

        await ReplaceAsync(player, null);
    }

    public async Task RelinkAsync(int? previousPlayerId, ApplicationUser user)
    {
        if (previousPlayerId == user.PlayerId)
        {
            return;
        }

        await ReleaseAsync(previousPlayerId);
        await ApplyAccountImageAsync(user);
    }

    public Task<PlayerLinkedAccountDto?> FindLinkedAccountAsync(int playerId)
    {
        return _userRepository.FirstOrDefaultAsync(new LinkedAccountByPlayerIdSpec(playerId));
    }

    public async Task<string?> CopyLinkedAccountImageAsync(int playerId)
    {
        var account = await FindLinkedAccountAsync(playerId);
        if (string.IsNullOrEmpty(account?.ProfileImage))
        {
            return null;
        }

        return await _imageService.CopyProfileImage(account.ProfileImage);
    }

    public async Task<int> BackfillAsync()
    {
        var users = await _userRepository.ListAsync(new AccountsAwaitingPlayerImageSpec());
        var applied = 0;
        foreach (var user in users)
        {
            if (await ApplyAccountImageAsync(user))
            {
                applied++;
            }
        }

        if (applied > 0)
        {
            _logger.LogInformation("Gave {Count} linked players the profile picture of their account", applied);
        }

        return applied;
    }

    private async Task<bool> ReplaceAsync(Player player, string? accountImage)
    {
        var previous = player.ImageSource == PlayerImageSource.Account ? player.Image : null;
        string? copy = null;
        if (!string.IsNullOrEmpty(accountImage))
        {
            copy = await _imageService.CopyProfileImage(accountImage);
            if (copy == null)
            {
                return false;
            }

            player.UseAccountImage(copy);
        }
        else if (previous == null)
        {
            return false;
        }
        else
        {
            player.ClearAccountImage();
        }

        try
        {
            await _unitOfWork.SaveChangesAsync();
        }
        catch (Exception)
        {
            _imageService.DeleteImage(copy);
            throw;
        }

        _imageService.DeleteImage(previous);
        _logger.LogInformation("Player {PlayerId} now follows the profile picture of its account", player.Id);
        return true;
    }
}
