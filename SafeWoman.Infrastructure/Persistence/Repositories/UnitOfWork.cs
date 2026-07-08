using Microsoft.EntityFrameworkCore.Storage;
using SafeWoman.Domain.Interfaces;

namespace SafeWoman.Infrastructure.Persistence.Repositories;

public class UnitOfWork : IUnitOfWork
{
    private readonly SafeWomanDbContext _context;

    public UnitOfWork(SafeWomanDbContext context) => _context = context;

    public Task<int> SaveChangesAsync(CancellationToken ct = default) =>
        _context.SaveChangesAsync(ct);

    public async Task<ITransaction> BeginTransactionAsync(CancellationToken ct = default)
    {
        var tx = await _context.Database.BeginTransactionAsync(ct);
        return new EfTransaction(tx);
    }

    public void Dispose() => _context.Dispose();

    private sealed class EfTransaction : ITransaction
    {
        private readonly IDbContextTransaction _tx;

        public EfTransaction(IDbContextTransaction tx) => _tx = tx;

        public Task CommitAsync(CancellationToken ct = default) => _tx.CommitAsync(ct);

        public Task RollbackAsync(CancellationToken ct = default) => _tx.RollbackAsync(ct);

        public ValueTask DisposeAsync() => _tx.DisposeAsync();
    }
}
