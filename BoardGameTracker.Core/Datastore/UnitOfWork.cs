using BoardGameTracker.Core.Datastore.Interfaces;
using Microsoft.EntityFrameworkCore.Storage;

namespace BoardGameTracker.Core.Datastore;

public class UnitOfWork : IUnitOfWork
{
    private readonly MainDbContext _context;

    public UnitOfWork(MainDbContext context)
    {
        _context = context;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _context.SaveChangesAsync(cancellationToken);
    }

    public Task<IDbContextTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        return _context.Database.BeginTransactionAsync(cancellationToken);
    }
}
