using System.Linq.Expressions;
using Charles.PaymentProcessor.Domain.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace PaymentSystem.Infrastructure.Repository;
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly PaymentDbContext _db;
    protected readonly DbSet<T> _set;

    public Repository(PaymentDbContext db)
    {
        _db = db;
        _set = db.Set<T>();
    }

    public async Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        await _set.FindAsync(new object?[] { id }, ct);

    public async Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default) =>
        await _set.ToListAsync(ct);

    public async Task AddAsync(T entity, CancellationToken ct = default)
    {
        await _set.AddAsync(entity, ct);
        await _db.SaveChangesAsync(ct);
    }

    public async Task UpdateAsync(T entity, CancellationToken ct = default)
    {
        _set.Update(entity);
        await _db.SaveChangesAsync(ct);
    }

    public async Task DeleteAsync(T entity, CancellationToken ct = default)
    {
        _set.Remove(entity);
        await _db.SaveChangesAsync(ct);
    }
    public virtual async Task<T?> GetSingleByAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default)
    {
        return await _set.FirstOrDefaultAsync(predicate, cancellationToken: ct);
    }

    public IQueryable<T> Query() => _set.AsQueryable();
}