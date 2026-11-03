using System.Linq.Expressions;

namespace LeaveDesk.Application.Interfaces;

public interface IRepository<T> where T : class
{
    Task<IEnumerable<T>> GetAllAsync();
    Task<T?> GetByIdAsync(int id);
    Task<T?> FirstOrDefaultAsync(Expression<Func<T, bool>> predicate);
    Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate);
    Task AddAsync(T entity);
    Task UpdateAsync(T entity);
    Task DeleteAsync(T entity);
}

public class RepositoryDeleteConflictException : Exception
{
    public RepositoryDeleteConflictException(string message, Exception inner) : base(message, inner)
    {
    }
}
