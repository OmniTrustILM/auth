using Auth.Common.Data.Repositories;
using Auth.Models.Entities;

namespace Auth.Data.Contracts
{
    public interface IPermissionRepository : IBaseRepository<Permission>
    {
        Task<List<Permission>> GetUserPermissions(Guid userUuid);

        Task<List<Permission>> GetRolePermissions(Guid roleUuid);
        Task<List<Permission>> GetRoleResourcePermissions(Guid roleUuid, Guid resourceUuid);
        Task<List<Permission>> GetRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid);
        void DeleteRolePermissionsWithoutObjects(Guid roleUuid);
        void DeleteRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid);
        void DeleteRoleResourceObjectPermissions(Guid roleUuid, Guid resourceUuid, Guid objectUuid);
    }
}
