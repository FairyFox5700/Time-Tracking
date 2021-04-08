using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Pagination;

namespace TimeTracking.Common.Abstract.Repository
{
    public interface IRepository<TKey, TEntity> where TEntity : class
    {
        Task<TEntity> AddAsync(TEntity entity);
        IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate);
        [Obsolete]
        Task<IEnumerable<TEntity>> GetAllAsync();
        Task<PagedResult<TEntity>> GetAllPagedAsync(int page = 0, int size = 0);
        Task<TEntity> GetByIdAsync(TKey id);
        Task<TEntity> UpdateAsync(TEntity updated);
        Task<int> DeleteAsync(TEntity t);
        Task<int> CountAsync();
        Task<TEntity> FilterOneAsync(Expression<Func<TEntity, bool>> filter = null);
        Task<IEnumerable<TEntity>> Filter(Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>,
            IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? page = null,
            int? pageSize = null);

        Task<bool> Exist(Expression<Func<TEntity, bool>> predicate);
    }

    public interface IDbContext
    {
        DbSet<T> Set<T>() where T : class;
    }
}