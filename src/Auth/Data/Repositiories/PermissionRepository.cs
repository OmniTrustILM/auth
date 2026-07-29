using Auth.Common.Data.Repositories;
using Auth.Data.Contracts;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data.Repositiories
{
    public class PermissionRepository : BaseRepository<Permission>, IPermissionRepository
    {
        public PermissionRepository(AuthDbContext repositoryContext) : base(repositoryContext)
        {

        }

        public async Task<List<Permission>> GetUserPermissions(Guid userUuid)
        {
            return await _dbSet.IgnoreAutoIncludes().Include(p => p.Resource).Include(p => p.Action).Include(p => p.Role).ThenInclude(r => r.Users.Where(u => u.Uuid == userUuid)).Where(p => p.Role.Users.Any(u => u.Uuid == userUuid)).OrderByDescending(p => p.ResourceUuid).ThenByDescending(p => p.ActionUuid).ThenByDescending(p => p.ObjectUuid).ToListAsync();
        }

        public async Task<List<Permission>> GetRolePermissions(Guid roleUuid)
        {
            return await _dbSet.IgnoreAutoIncludes().Include(p => p.Resource).Include(p => p.Action).Where(p => p.RoleUuid == roleUuid).OrderByDescending(p => p.ResourceUuid).ThenByDescending(p => p.ActionUuid).ThenByDescending(p => p.ObjectUuid).ToListAsync();
        }

        public async Task<List<Permission>> GetRoleResourcePermissions(Guid roleUuid, Guid resourceUuid)
        {
            return await _dbSet.IgnoreAutoIncludes().Include(p => p.Resource).Include(p => p.Action).Where(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid).OrderByDescending(p => p.ResourceUuid).ThenByDescending(p => p.ActionUuid).ThenByDescending(p => p.ObjectUuid).ToListAsync();
        }

        public async Task<List<Permission>> GetRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid)
        {
            return await _dbSet.IgnoreAutoIncludes().Where(p => p.ObjectUuid != null && p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid).OrderByDescending(p => p.ResourceUuid).ThenByDescending(p => p.ActionUuid).ThenByDescending(p => p.ObjectUuid).ToListAsync();
        }

        public void DeleteRolePermissionsWithoutObjects(Guid roleUuid)
        {
            _dbSet.RemoveRange(_dbSet.Where(p => p.RoleUuid == roleUuid && p.ObjectUuid == null));
        }

        public void DeleteRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid)
        {
            _dbSet.RemoveRange(_dbSet.Where(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid && p.ObjectUuid != null));
        }

        public void DeleteRoleResourceObjectPermissions(Guid roleUuid, Guid resourceUuid, Guid objectUuid)
        {
            _dbSet.RemoveRange(_dbSet.Where(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid && p.ObjectUuid == objectUuid));
        }
    }
}
