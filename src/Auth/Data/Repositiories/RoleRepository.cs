using Auth.Common.Data.Repositories;
using Auth.Data.Contracts;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositiories
{
    public class RoleRepository : BaseRepository<Role>, IRoleRepository
    {
        public RoleRepository(AuthDbContext repositoryContext) : base(repositoryContext, null, new[] { "Users" })
        {
        }

        public async Task<IEnumerable<Role>> GetUserRolesAsync(Guid userUuid)
        {
            return await _dbSet.Include(r => r.Users).Where(r => r.Users.Any(u => u.Uuid == userUuid)).ToListAsync();
        }
    }
}
