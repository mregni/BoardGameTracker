using BoardGameTracker.Common.Entities.Helpers;

namespace BoardGameTracker.Core.Datastore.Interfaces;

/// <summary>
/// Full read/write repository for id-keyed entities. The write methods deliberately do NOT
/// call SaveChanges — persistence stays under <see cref="IUnitOfWork"/>, exactly as CrudHelper
/// behaved. This preserves the deferred-save flow the badge evaluation and batch import rely on.
/// </summary>
public interface IRepository<T> : IReadRepository<T> where T : HasId
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<T> CreateAsync(T entity);
    Task CreateRangeAsync(List<T> entities);
    Task<T> Update(T entity);
    Task<bool> DeleteAsync(int id);
}
