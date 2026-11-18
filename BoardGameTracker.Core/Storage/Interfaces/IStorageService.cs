using BoardGameTracker.Common.DTOs.Commands;
using BoardGameTracker.Common.Entities;

namespace BoardGameTracker.Core.Storage.Interfaces;

public interface IStorageService
{
    Task<List<StorageLocation>> GetAll();
    Task<StorageLocation> Create(CreateStorageLocationCommand command);
    Task<StorageLocation> Update(int id, UpdateStorageLocationCommand command);
    Task<StorageLocation> Move(int id, MoveStorageLocationCommand command);
    Task Delete(int id);
    Task Assign(AssignStorageCommand command);
    Task Arrange(ArrangeStorageCommand command);
    Task UpdateBoxOrientation(UpdateBoxOrientationCommand command);
}
