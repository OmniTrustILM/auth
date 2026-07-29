using Auth.Common.Data.Repositories;
using Auth.Models.Entities;

namespace Auth.Data.Contracts
{
    public interface IResourceRepository : IBaseRepository<Resource>
    {
        Task<List<Resource>> GetResourcesWithActions();

        Task<Dictionary<string, Resource>> GetResourcesWithActionsMap();

        Task<Dictionary<TKey, Resource>> GetResourcesMapAsync<TKey>(Func<Resource, TKey> keySelector) where TKey : notnull;
    }
}
