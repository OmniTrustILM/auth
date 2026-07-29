using System.Linq.Expressions;
using Auth.Common.Data;
using Auth.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Common.Data.Repositories
{
    public interface IBaseRepository<TEntity> where TEntity : class, IBaseEntity, new()
    {
        IQueryable<TEntity> FindAll();
        IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression);

        Task<PagedList<TEntity>> GetAllAsync(QueryStringParameters parameters);
        Task<PagedList<TEntity>> GetWhereAsync(QueryStringParameters parameters, Expression<Func<TEntity, bool>> expression);
        Task<TEntity> GetByKeyAsync(Guid entityKey);
        Task<TEntity?> GetByConditionAsync(Expression<Func<TEntity, bool>> expression);
        Task<IEnumerable<TEntity>> GetByUuidsAsync(IEnumerable<Guid> uuids);
        public Task<Dictionary<TKey, TEntity>> GetDictionaryMap<TKey>(Func<TEntity, TKey> keySelector, Expression<Func<TEntity, bool>>? expression = null) where TKey : notnull;

        void Create(TEntity entity);
        Task UpdateAsync(Guid entityKey, TEntity entity);
        Task DeleteAsync(Guid entityKey);
        void Delete(TEntity entity);
        void Reload(TEntity entity);
    }
}
