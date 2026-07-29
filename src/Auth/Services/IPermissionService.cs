using Auth.Common.Services;
using Auth.Models.Dto;

namespace Auth.Services
{
    public interface IPermissionService
    {
        Task<SubjectPermissionsDto> GetRolePermissionsAsync(Guid roleUuid);

        Task<ResourcePermissionsDto> GetRoleResourcesPermissionsAsync(Guid roleUuid, Guid resourceUuid);

        Task<SubjectPermissionsDto> SaveRolePermissionsAsync(Guid roleUuid, RolePermissionsRequestDto rolePermissions);


        Task<List<ObjectPermissionsDto>> GetRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid);

        Task SaveRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid, List<ObjectPermissionsRequestDto> objectsPermissions);

        Task SaveRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid, ObjectPermissionsRequestDto objectPermissions);

        Task DeleteRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid);

        Task<SubjectPermissionsDto> GetUserPermissionsAsync(Guid userUuid);
    }
}
