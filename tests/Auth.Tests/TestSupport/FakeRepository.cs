using Auth.Common.Data;
using Auth.Common.Data.Repositories;
using Auth.Common.Exceptions;
using Auth.Common.Extensions;
using Auth.Common.Models.Entities;
using System.Linq.Expressions;

namespace Auth.Tests.TestSupport;

/// <summary>
/// In-memory <see cref="IBaseRepository{TEntity}"/> that separates staged writes from the visible store, the way a
/// change tracker separates them from the database: <see cref="Create"/> and the deletes only take effect once
/// <see cref="FakeRepositoryManager.SaveAsync"/> flushes them. Services that create an entity and then read it back
/// therefore exercise the same ordering they do against EF, and a save that throws leaves the writes staged.
/// </summary>
public class FakeRepository<TEntity> : IBaseRepository<TEntity>
    where TEntity : class, IBaseEntity, new()
{
    private readonly List<TEntity> _stored = [];
    private readonly List<TEntity> _stagedCreates = [];
    private readonly List<TEntity> _stagedDeletes = [];

    public IReadOnlyList<TEntity> Stored => _stored;
    public IReadOnlyList<TEntity> StagedCreates => _stagedCreates;
    public IReadOnlyList<TEntity> StagedDeletes => _stagedDeletes;
    public int ReloadCount { get; private set; }

    public FakeRepository<TEntity> Seed(params TEntity[] entities)
    {
        _stored.AddRange(entities);
        return this;
    }

    public void Flush()
    {
        _stored.AddRange(_stagedCreates);
        foreach (var deleted in _stagedDeletes) _stored.Remove(deleted);

        _stagedCreates.Clear();
        _stagedDeletes.Clear();
    }

    public bool Detach(object entity)
    {
        if (entity is not TEntity typed) return false;

        return _stagedCreates.Remove(typed) || _stagedDeletes.Remove(typed);
    }

    public IQueryable<TEntity> FindAll() => _stored.AsQueryable();

    public IQueryable<TEntity> FindByCondition(Expression<Func<TEntity, bool>> expression)
        => _stored.AsQueryable().Where(expression);

    public Task<PagedList<TEntity>> GetAllAsync(QueryStringParameters parameters) => Page(FindAll(), parameters);

    public Task<PagedList<TEntity>> GetWhereAsync(QueryStringParameters parameters, Expression<Func<TEntity, bool>> expression)
        => Page(FindByCondition(expression), parameters);

    public Task<TEntity> GetByKeyAsync(Guid entityKey) => Task.FromResult(Require(entityKey, "find"));

    public Task<TEntity?> GetByConditionAsync(Expression<Func<TEntity, bool>> expression)
        => Task.FromResult(_stored.FirstOrDefault(expression.Compile()));

    public Task<IEnumerable<TEntity>> GetByUuidsAsync(IEnumerable<Guid> uuids)
    {
        var wanted = uuids.ToHashSet();
        return Task.FromResult<IEnumerable<TEntity>>(_stored.Where(e => wanted.Contains(e.Uuid)).ToList());
    }

    public Task<Dictionary<TKey, TEntity>> GetDictionaryMap<TKey>(Func<TEntity, TKey> keySelector) where TKey : notnull
        => Task.FromResult(_stored.ToDictionary(keySelector));

    public void Create(TEntity entity) => _stagedCreates.Add(entity);

    public Task UpdateAsync(Guid entityKey, TEntity entity)
    {
        var tracked = Require(entityKey, "update");
        entity.Uuid = tracked.Uuid;
        _stored[_stored.IndexOf(tracked)] = entity;

        return Task.CompletedTask;
    }

    public Task DeleteAsync(Guid entityKey)
    {
        _stagedDeletes.Add(Require(entityKey, "delete"));
        return Task.CompletedTask;
    }

    public void Delete(TEntity entity) => _stagedDeletes.Add(entity);

    public void Reload(TEntity entity) => ReloadCount++;

    private TEntity Require(Guid entityKey, string operation)
    {
        var entity = _stored.FirstOrDefault(e => e.Uuid == entityKey);
        if (entity == null) throw new EntityNotFoundException($"Cannot {operation} entity {typeof(TEntity).Name} with id {entityKey}");

        return entity;
    }

    // The dynamic sort runs through the production extension so an unsortable field fails here exactly as it would
    // against a database.
    private static Task<PagedList<TEntity>> Page(IQueryable<TEntity> query, QueryStringParameters parameters)
    {
        if (parameters.SortBy != null) query = query.OrderBy(parameters.SortBy, parameters.SortAscending);

        return Task.FromResult(PagedList<TEntity>.CreateFromFullList(query.ToList(), parameters.Page, parameters.PageSize));
    }
}
