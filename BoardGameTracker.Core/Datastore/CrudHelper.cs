using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

public abstract class CrudHelper<T>: ICrudHelper<T> where T : HasId
{
    private readonly DbSet<T> _dbSet;

    protected CrudHelper(MainDbContext context)
    {
        _dbSet = context.Set<T>();
    }
    
    public virtual Task<T?> GetByIdAsync(int id)
    {
        return _dbSet.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual Task<List<T>> GetAllAsync()
    {
        return _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        return entity;
    }

    public async Task CreateRangeAsync(List<T> obj)
    {
        await _dbSet.AddRangeAsync(obj);
    }

    public virtual Task<T> UpdateAsync(T entity)
    {
        _dbSet.Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _dbSet.Remove(entity);
        return true;
    }
}