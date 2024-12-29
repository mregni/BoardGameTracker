namespace BoardGameTracker.Core.Datastore.Interfaces;

public interface ICrudHelper<T> where T : class
{
    Task<T?> GetByIdAsync(int id);
    Task<List<T>> GetAllAsync();
    Task<T> CreateAsync(T obj);
    Task CreateRangeAsync(List<T> obj);
    Task<bool> DeleteAsync(int id);
    Task<T> UpdateAsync(T entity);
}