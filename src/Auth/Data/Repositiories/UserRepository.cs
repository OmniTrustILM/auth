using Auth.Common.Data.Repositories;
using Auth.Data.Contracts;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositiories
{
    public class UserRepository : BaseRepository<User>, IUserRepository
    {
        public UserRepository(AuthDbContext repositoryContext) : base(repositoryContext, null, new[] { "Roles" })
        {
        }

        public async Task<IEnumerable<User>> GetRoleUsersAsync(Guid roleUuid)
        {
            return await _dbSet.Include(u => u.Roles).Where(u => u.Roles.Any(r => r.Uuid == roleUuid)).ToListAsync();
        }
    }
}
