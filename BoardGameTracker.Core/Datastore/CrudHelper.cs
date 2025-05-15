using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

public abstract class CrudHelper<T>: ICrudHelper<T> where T : HasId
{
    private readonly MainDbContext _context;
    private readonly DbSet<T> _dbSet;

    protected CrudHelper(MainDbContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }
    
    public virtual Task<T?> GetByIdAsync(int id)
    {
        return _dbSet.FirstOrDefaultAsync(x => x.Id == id);
    }

    public virtual Task<List<T>> GetAllAsync()
    {
        return _dbSet.AsNoTracking().ToListAsync();
    }

    public virtual async Task<T> CreateAsync(T entity)
    {
        await _dbSet.AddAsync(entity);
        await _context.SaveChangesAsync();

        return entity;
    }

    public async Task CreateRangeAsync(List<T> obj)
    {
        await _dbSet.AddRangeAsync(obj);
        await _context.SaveChangesAsync();
    }

    public virtual async Task<T> UpdateAsync(T entity)
    {
        await _context.SaveChangesAsync();

        return entity;
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await _dbSet.FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync();
        return true;
    }
}