using BoardGameTracker.Common;
using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;
using BoardGameTracker.Common.Enums;
using BoardGameTracker.Common.Exceptions;
using BoardGameTracker.Core.Datastore.Interfaces;
using BoardGameTracker.Core.Locations.Interfaces;
using BoardGameTracker.Core.Locations.Specifications;
using BoardGameTracker.Core.Storage.Interfaces;
using Microsoft.Extensions.Logging;

namespace BoardGameTracker.Core.Storage;

public class StorageService : IStorageService
{
    private readonly IStorageRepository _storageRepository;
    private readonly ILocationRepository _locationRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ILogger<StorageService> _logger;

    public StorageService(
        IStorageRepository storageRepository,
        ILocationRepository locationRepository,
        IUnitOfWork unitOfWork,
        ILogger<StorageService> logger)
    {
        _storageRepository = storageRepository;
        _locationRepository = locationRepository;
        _unitOfWork = unitOfWork;
        _logger = logger;
    }

    public Task<List<StorageLocation>> GetAll()
    {
        return _storageRepository.GetAllAsync();
    }

    public async Task<StorageLocation> Create(CreateStorageLocationCommand command)
    {
        await EnsureLocationExists(command.LocationId);
        if (command.ParentId != null)
        {
            var parent = await _storageRepository.GetByIdAsync(command.ParentId.Value)
                         ?? throw new EntityNotFoundException(nameof(StorageLocation), command.ParentId.Value);
            if (parent.Layout != StorageLayout.Free || parent.LocationId != command.LocationId)
            {
                throw new DomainException(Constants.Errors.StorageInvalidParent);
            }
        }

        var storageLocation = new StorageLocation(command.Name, command.LocationId, command.ParentId, command.Layout, command.Rows, command.Columns);
        await _storageRepository.CreateAsync(storageLocation);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Storage location {StorageLocationId} created in location {LocationId}", storageLocation.Id, storageLocation.LocationId);

        return storageLocation;
    }

    public async Task<StorageLocation> Update(int id, UpdateStorageLocationCommand command)
    {
        var all = await _storageRepository.GetAllTrackedAsync();
        var storageLocation = Find(all, id);
        storageLocation.Rename(command.Name);

        if (storageLocation.IsCabinet && command.Rows != null)
        {
            var removed = storageLocation.Resize(command.Rows.Value, command.Columns ?? storageLocation.Columns ?? 1);
            await MoveItems(removed.Select(x => x.Id).ToList(), storageLocation.LocationId, storageLocation.Id);
            foreach (var slot in removed)
            {
                await _storageRepository.DeleteAsync(slot.Id);
            }
        }

        await _unitOfWork.SaveChangesAsync();
        return storageLocation;
    }

    public async Task<StorageLocation> Move(int id, MoveStorageLocationCommand command)
    {
        await EnsureLocationExists(command.LocationId);
        var all = await _storageRepository.GetAllTrackedAsync();
        var storageLocation = Find(all, id);
        var parent = command.ParentId == null ? null : Find(all, command.ParentId.Value);
        if (storageLocation.Layout == StorageLayout.Slot)
        {
            throw new DomainException(Constants.Errors.StorageInvalidParent);
        }

        var previousLocationId = storageLocation.LocationId;
        storageLocation.MoveTo(command.LocationId, parent);
        if (previousLocationId != command.LocationId)
        {
            var subtree = all.Where(x => x.IsWithin(storageLocation)).Select(x => x.Id).ToList();
            await MoveItems(subtree, command.LocationId, null, keepStorageLocation: true);
        }

        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Storage location {StorageLocationId} moved to location {LocationId}", id, command.LocationId);
        return storageLocation;
    }

    public async Task Delete(int id)
    {
        var all = await _storageRepository.GetAllTrackedAsync();
        var storageLocation = Find(all, id);
        var subtree = all.Where(x => x.IsWithin(storageLocation)).Select(x => x.Id).ToList();
        await MoveItems(subtree, storageLocation.LocationId, storageLocation.ParentId);
        await _storageRepository.DeleteAsync(id);
        await _unitOfWork.SaveChangesAsync();
        _logger.LogInformation("Storage location {StorageLocationId} deleted", id);
    }

    public async Task Assign(AssignStorageCommand command)
    {
        var (games, expansions) = await LoadItems(command.Items);
        if (command.StorageLocationId == null && command.LocationId == null)
        {
            games.ForEach(x => x.ClearStorage());
            expansions.ForEach(x => x.ClearStorage());
            await _unitOfWork.SaveChangesAsync();
            return;
        }

        int locationId;
        if (command.StorageLocationId != null)
        {
            var storageLocation = await _storageRepository.GetByIdAsync(command.StorageLocationId.Value)
                                  ?? throw new EntityNotFoundException(nameof(StorageLocation), command.StorageLocationId.Value);
            locationId = storageLocation.LocationId;
        }
        else
        {
            locationId = command.LocationId!.Value;
            await EnsureLocationExists(locationId);
        }

        var position = await _storageRepository.GetNextPositionAsync(locationId, command.StorageLocationId);
        foreach (var item in command.Items)
        {
            Store(item, games, expansions, locationId, command.StorageLocationId, position++);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task Arrange(ArrangeStorageCommand command)
    {
        var storageLocation = await _storageRepository.GetByIdAsync(command.StorageLocationId)
                              ?? throw new EntityNotFoundException(nameof(StorageLocation), command.StorageLocationId);
        var (games, expansions) = await LoadItems(command.Items);
        for (var position = 0; position < command.Items.Count; position++)
        {
            Store(command.Items[position], games, expansions, storageLocation.LocationId, storageLocation.Id, position);
        }

        await _unitOfWork.SaveChangesAsync();
    }

    public async Task UpdateBoxOrientation(UpdateBoxOrientationCommand command)
    {
        var (games, expansions) = await LoadItems([command.Item]);
        games.ForEach(x => x.UpdateBoxOrientation(command.Orientation));
        expansions.ForEach(x => x.UpdateBoxOrientation(command.Orientation));
        await _unitOfWork.SaveChangesAsync();
    }

    private static void Store(StorageItemReference item, List<Game> games, List<Expansion> expansions, int locationId, int? storageLocationId, int position)
    {
        if (item.Type == StorageItemType.Game)
        {
            games.First(x => x.Id == item.Id).Store(locationId, storageLocationId, position);
        }
        else
        {
            expansions.First(x => x.Id == item.Id).Store(locationId, storageLocationId, position);
        }
    }

    private async Task<(List<Game> Games, List<Expansion> Expansions)> LoadItems(List<StorageItemReference> items)
    {
        var gameIds = items.Where(x => x.Type == StorageItemType.Game).Select(x => x.Id).Distinct().ToList();
        var expansionIds = items.Where(x => x.Type == StorageItemType.Expansion).Select(x => x.Id).Distinct().ToList();
        var games = await _storageRepository.GetGamesAsync(gameIds);
        var expansions = await _storageRepository.GetExpansionsAsync(expansionIds);

        var missingGame = gameIds.Except(games.Select(x => x.Id)).FirstOrDefault();
        if (missingGame != 0)
        {
            throw new EntityNotFoundException(nameof(Game), missingGame);
        }

        var missingExpansion = expansionIds.Except(expansions.Select(x => x.Id)).FirstOrDefault();
        if (missingExpansion != 0)
        {
            throw new EntityNotFoundException(nameof(Expansion), missingExpansion);
        }

        return (games, expansions);
    }

    private async Task MoveItems(List<int> from, int locationId, int? storageLocationId, bool keepStorageLocation = false)
    {
        foreach (var game in await _storageRepository.GetGamesStoredInAsync(from))
        {
            game.Store(locationId, keepStorageLocation ? game.StorageLocationId : storageLocationId, game.StoragePosition);
        }

        foreach (var expansion in await _storageRepository.GetExpansionsStoredInAsync(from))
        {
            expansion.Store(locationId, keepStorageLocation ? expansion.StorageLocationId : storageLocationId, expansion.StoragePosition);
        }
    }

    private async Task EnsureLocationExists(int locationId)
    {
        if (!await _locationRepository.AnyAsync(new LocationByIdSpec(locationId)))
        {
            throw new EntityNotFoundException(nameof(Location), locationId);
        }
    }

    private static StorageLocation Find(List<StorageLocation> all, int id)
    {
        return all.FirstOrDefault(x => x.Id == id) ?? throw new EntityNotFoundException(nameof(StorageLocation), id);
    }
}
