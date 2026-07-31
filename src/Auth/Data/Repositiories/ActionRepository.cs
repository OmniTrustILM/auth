using Auth.Common.Data.Repositories;
using Auth.Data.Contracts;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositiories
{
    public class ActionRepository : BaseRepository<Models.Entities.Action>, IActionRepository
    {
        public ActionRepository(AuthDbContext repositoryContext) : base(repositoryContext)
        {
        }

        public async Task<Dictionary<TKey, Models.Entities.Action>> GetActionsMapAsync<TKey>(Func<Models.Entities.Action, TKey> keySelector) where TKey : notnull
        {
            return await _dbSet.Include(a => a.Permissions).AsTracking().ToDictionaryAsync(keySelector);
        }
    }
}
