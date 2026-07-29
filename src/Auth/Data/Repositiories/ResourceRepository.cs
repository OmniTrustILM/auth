using Auth.Common.Data.Repositories;
using Auth.Data.Contracts;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace Auth.Data.Repositiories
{
    public class ResourceRepository : BaseRepository<Resource>, IResourceRepository
    {
        public ResourceRepository(AuthDbContext repositoryContext) : base(repositoryContext, null, new[] { "Actions" })
        {
        }

        public async Task<List<Resource>> GetResourcesWithActions()
        {
            return await _dbSet.Include(r => r.Actions).OrderBy(r => r.DisplayName).ToListAsync();
        }

        public async Task<Dictionary<string, Resource>> GetResourcesWithActionsMap()
        {
            return await _dbSet.Include(r => r.Actions).OrderBy(r => r.DisplayName).ToDictionaryAsync(r => r.Name);
        }

        public async Task<Dictionary<TKey, Resource>> GetResourcesMapAsync<TKey>(Func<Resource, TKey> keySelector) where TKey : notnull
        {
            return await _dbSet.Include(r => r.Actions).Include(r => r.Permissions).AsTracking().ToDictionaryAsync(keySelector);
        }
    }
}
