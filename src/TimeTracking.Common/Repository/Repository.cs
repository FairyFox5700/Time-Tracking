using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using TimeTracking.Common.Abstract.Repository;
using TimeTracking.Common.Pagination;

namespace TimeTracking.Common.Repository
{
    public abstract class Repository<TKey, TEntity, TContext> : IRepository<TKey, TEntity> where TEntity : class
        where TContext : DbContext, IDbContext
    {
        protected readonly TContext _dbContext;
        protected readonly DbSet<TEntity> _dbSet;

        protected Repository(TContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
            _dbSet = dbContext.Set<TEntity>();
        }

        public async Task<TEntity> AddAsync(TEntity entity)
        {
            if (entity == null)
                throw new ArgumentNullException(nameof(entity));
            await _dbSet.AddAsync(entity);
            await _dbContext.SaveChangesAsync();
            return entity;
        }

        public IQueryable<TEntity> Find(Expression<Func<TEntity, bool>> predicate)
        {
            return _dbSet.Where(predicate);
        }

        public async Task<IEnumerable<TEntity>> GetAllAsync()
        {
            return await _dbSet.ToListAsync();
        }

        public async Task<PagedResult<TEntity>> GetAllPagedAsync(int page = 0, int size = 0)
        {
            return await _dbSet.PaginateAsync(page, size);
        }

        public async Task<TEntity> GetByIdAsync(TKey id)
        {
            return await _dbSet.FindAsync(id);
        }

        public async Task<TEntity> UpdateAsync(TEntity updated)
        {
            if (updated == null)
            {
                return null;
            }

            _dbContext.Set<TEntity>().Attach(updated);
            _dbContext.Entry(updated).State = EntityState.Modified;
            await _dbContext.SaveChangesAsync();
            return updated;
        }

        public async Task<int> DeleteAsync(TEntity t)
        {
            _dbSet.Remove(t);
            return await _dbContext.SaveChangesAsync();
        }

        public async Task<int> CountAsync()
        {
            return await _dbContext.Set<TEntity>().CountAsync();
        }

        public async Task<TEntity> FilterOneAsync(Expression<Func<TEntity, bool>> filter = null)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();
            if (filter != null)
            {
                query = query.Where(filter);
            }

            return await query.FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TEntity>> Filter(Expression<Func<TEntity, bool>> filter = null,
            Func<IQueryable<TEntity>,
                IOrderedQueryable<TEntity>> orderBy = null,
            string includeProperties = "",
            int? page = null,
            int? pageSize = null)
        {
            IQueryable<TEntity> query = _dbContext.Set<TEntity>();
            if (filter != null)
            {
                query = query.Where(filter);
            }

            if (orderBy != null)
            {
                query = orderBy(query);
            }

            if (includeProperties != null)
            {
                foreach (
                    var includeProperty in includeProperties.Split(new[] { ',' }, StringSplitOptions.RemoveEmptyEntries))
                {
                    query = query.Include(includeProperty);
                }
            }

            if (page != null && pageSize != null)
            {
                query = query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value);
            }

            return await query.ToListAsync();
        }


        public async Task<bool> Exist(Expression<Func<TEntity, bool>> predicate)
        {
            var exist = _dbContext.Set<TEntity>().Where(predicate);
            return await exist.AnyAsync();
        }
    }
}