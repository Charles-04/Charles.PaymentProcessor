using System.Linq.Expressions;

namespace Charles.PaymentProcessor.Domain.Interfaces;

public interface IRepository<T> where T : class
{
    Task<T?> GetByIdAsync(Guid id, CancellationToken ct = default);
    Task<IEnumerable<T>> GetAllAsync(CancellationToken ct = default);
    Task AddAsync(T entity, CancellationToken ct = default);
    Task UpdateAsync(T entity, CancellationToken ct = default);
    Task DeleteAsync(T entity, CancellationToken ct = default);
    Task<T?> GetSingleByAsync(Expression<Func<T, bool>> predicate, CancellationToken ct = default);

    IQueryable<T> Query(); // for more flexible queries
}