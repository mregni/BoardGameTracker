using BoardGameTracker.Common.Entities.Helpers;
using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace BoardGameTracker.Core.Datastore;

/// <summary>
/// Full read/write specification-driven repository for id-keyed entities. Create/CreateRange/Delete
/// mutate the change tracker only and never call SaveChanges — persistence stays under IUnitOfWork.
/// </summary>
public class EfRepository<T> : EfReadRepository<T>, IRepository<T> where T : HasId
{
    public EfRepository(MainDbContext context) : base(context)
    {
    }

    public virtual Task<T?> GetByIdAsync(int id)
        => Context.Set<T>().FirstOrDefaultAsync(x => x.Id == id);

    public virtual Task<List<T>> GetAllAsync()
        => Context.Set<T>().AsNoTracking().ToListAsync();

    public virtual async Task<T> CreateAsync(T entity)
    {
        await Context.Set<T>().AddAsync(entity);
        return entity;
    }

    public async Task CreateRangeAsync(List<T> entities)
    {
        await Context.Set<T>().AddRangeAsync(entities);
    }

    public virtual Task<T> Update(T entity)
    {
        Context.Set<T>().Update(entity);
        return Task.FromResult(entity);
    }

    public virtual async Task<bool> DeleteAsync(int id)
    {
        var entity = await Context.Set<T>().FindAsync(id);
        if (entity == null)
        {
            return false;
        }

        Context.Set<T>().Remove(entity);
        return true;
    }
}
