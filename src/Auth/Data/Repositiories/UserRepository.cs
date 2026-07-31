using Auth.Common.Data;
using Auth.Common.Data.Repositories;
using Auth.Common.Extensions;
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

        public async Task<List<User>> GetGroupMembersAsync(string groupName, QueryStringParameters parameters)
        {
            IQueryable<User> query = _dbSet.AsNoTracking();
            if (parameters.SortBy != null) query = query.OrderBy(parameters.SortBy, parameters.SortAscending);

            var users = await query.ToListAsync();

            return users.Where(u => u.Groups != null && u.Groups.Exists(g => g.Name.Equals(groupName))).ToList();
        }
    }
}
