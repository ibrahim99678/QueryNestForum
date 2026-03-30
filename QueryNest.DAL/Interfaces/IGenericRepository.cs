using System.Linq.Expressions;

namespace QueryNest.DAL.Interfaces;

public interface IGenericRepository<T> where T : class
{
    Task<T?> GetByIdAsync(CancellationToken cancellationToken = default, params object[] keyValues);
    Task<List<T>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<List<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);
    IQueryable<T> Query();
    Task AddAsync(T entity, CancellationToken cancellationToken = default);
    void Update(T entity);
    void Remove(T entity);
}
