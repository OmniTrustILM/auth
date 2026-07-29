using Auth.Common.Exceptions;
using Auth.Common.Extensions;
using Auth.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Auth.Common.Data.Repositories
{
    public abstract class BaseRepository<TEntity> : IBaseRepository<TEntity> where TEntity : BaseEntity, new()
    {
        protected DbContext _dbContext;
        protected DbSet<TEntity> _dbSet;

        protected readonly string[]? _includes = null;
        protected readonly string[]? _includesDetail = null;

        public BaseRepository(DbContext repositoryContext)
        {
            _dbContext = repositoryContext;
            _dbSet = repositoryContext.Set<TEntity>();
        }

        public BaseRepository(DbContext repositoryContext, string[]? includes, string[]? includesDetail)
            : this(repositoryContext)
        {
            _includes = includes;
            _includesDetail = includesDetail;
        }

        #region Get operations for read-only purpose (No Tracking)

        public IQueryable<TEntity> FindAll()
        {
            return QueryWithCrudIncludes(false).AsNoTracking();
        }

        public IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression)
        {
            return QueryWithCrudIncludes(false).Where(expression).AsNoTracking();
        }

        public async Task<PagedList<TEntity>> GetAllAsync(QueryStringParameters parameters)
        {
            var queryable = FindAll();
            if (parameters.SortBy != null) queryable = queryable.OrderBy(parameters.SortBy, parameters.SortAscending);
            return await PagedList<TEntity>.CreateAsync(queryable, parameters.Page, parameters.PageSize);
        }

        public async Task<PagedList<TEntity>> GetWhereAsync(QueryStringParameters parameters, Expression<Func<TEntity, bool>> expression)
        {
            var queryable = FindByCondition(expression);
            if (parameters.SortBy != null) queryable = queryable.OrderBy(parameters.SortBy, parameters.SortAscending);
            return await PagedList<TEntity>.CreateAsync(queryable, parameters.Page, parameters.PageSize);
        }

        #endregion

        public async Task<TEntity> GetByKeyAsync(Guid entityKey)
        {
            return await GetTrackedEntityByKey(entityKey, "find");
        }

        public async Task<TEntity?> GetByConditionAsync(Expression<Func<TEntity, bool>> expression)
        {
            var query = QueryWithCrudIncludes(true);
            return await query.Where(expression).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<TEntity>> GetByUuidsAsync(IEnumerable<Guid> uuids)
        {
            return await _dbSet.Where(e => uuids.Contains(e.Uuid)).ToListAsync();
        }

        public async Task<Dictionary<TKey, TEntity>> GetDictionaryMap<TKey>(Func<TEntity, TKey> keySelector, Expression<Func<TEntity, bool>>? expression = null) where TKey : notnull
        {
            var context = _dbSet;
            if (expression != null) context.Where(expression);
            return await context.ToDictionaryAsync(keySelector);
        }

        #region CRUD operations

        public void Create(TEntity entity)
        {
            _dbSet.Add(entity);
        }

        public async Task UpdateAsync(Guid entityKey, TEntity entity)
        {
            var trackedEntity = await GetTrackedEntityByKey(entityKey, "update");
            //entity.Id = trackedEntity.Id;
            entity.Uuid = trackedEntity.Uuid;

            _dbContext.Entry(trackedEntity).CurrentValues.SetValues(entity);
        }

        public async Task DeleteAsync(Guid entityKey)
        {
            var trackedEntity = await GetTrackedEntityByKey(entityKey, "delete");
            _dbSet.Remove(trackedEntity);
        }

        public void Delete(TEntity entity)
        {
            _dbSet.Remove(entity);
        }

        public void Reload(TEntity entity)
        {
            _dbContext.Entry(entity).Reload();
        }

        #endregion

        protected IQueryable<TEntity> QueryWithCrudIncludes(bool detailResponse)
        {
            var includes = detailResponse ? _includesDetail : _includes;
            if (includes == null || includes.Length == 0) return _dbSet;

            IQueryable<TEntity> query = _dbSet;
            foreach (var include in includes) query = query.Include(include);

            return query;
        }

        #region Private methods

        private async Task<TEntity> GetTrackedEntityByKey(Guid entityKey, string operation)
        {
            var query = QueryWithCrudIncludes(true);
            var trackedEntity = await query.FirstOrDefaultAsync(e => e.Uuid == entityKey);
            if (trackedEntity == null) throw new EntityNotFoundException($"Cannot {operation} entity {typeof(TEntity).Name} with id {entityKey}");

            return trackedEntity;
        }
        
        #endregion

    }
}
