using Auth.Models.Dto;
using Auth.Services;

namespace Auth.Tests.TestSupport;

/// <summary>
/// Records what its caller asked for and answers with whatever the test staged. Used where the caller's own branching is
/// under test and the permission merge is not.
/// </summary>
public sealed class FakePermissionService : IPermissionService
{
    public SubjectPermissionsDto SubjectPermissions { get; set; } = new();
    public ResourcePermissionsDto ResourcePermissions { get; set; } = new() { Name = "certificates" };

    public List<Guid> RolePermissionsRequested { get; } = [];
    public List<Guid> UserPermissionsRequested { get; } = [];
    public List<(Guid RoleUuid, RolePermissionsRequestDto Permissions)> SavedRolePermissions { get; } = [];
    public List<(Guid RoleUuid, Guid ResourceUuid, List<ObjectPermissionsRequestDto> Objects)> SavedObjectsPermissions { get; } = [];
    public List<(Guid RoleUuid, Guid ResourceUuid, Guid ObjectUuid, ObjectPermissionsRequestDto Permissions)> SavedObjectPermissions { get; } = [];
    public List<(Guid RoleUuid, Guid ResourceUuid, Guid ObjectUuid)> DeletedObjectPermissions { get; } = [];

    public Task<SubjectPermissionsDto> GetRolePermissionsAsync(Guid roleUuid)
    {
        RolePermissionsRequested.Add(roleUuid);
        return Task.FromResult(SubjectPermissions);
    }

    public Task<ResourcePermissionsDto> GetRoleResourcesPermissionsAsync(Guid roleUuid, Guid resourceUuid)
        => Task.FromResult(ResourcePermissions);

    public Task<SubjectPermissionsDto> SaveRolePermissionsAsync(Guid roleUuid, RolePermissionsRequestDto rolePermissions)
    {
        SavedRolePermissions.Add((roleUuid, rolePermissions));
        return Task.FromResult(SubjectPermissions);
    }

    public Task<List<ObjectPermissionsDto>> GetRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid)
        => Task.FromResult(ResourcePermissions.Objects);

    public Task SaveRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid, List<ObjectPermissionsRequestDto> objectsPermissions)
    {
        SavedObjectsPermissions.Add((roleUuid, resourceUuid, objectsPermissions));
        return Task.CompletedTask;
    }

    public Task SaveRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid, ObjectPermissionsRequestDto objectPermissions)
    {
        SavedObjectPermissions.Add((roleUuid, resourceUuid, objectUuid, objectPermissions));
        return Task.CompletedTask;
    }

    public Task DeleteRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid)
    {
        DeletedObjectPermissions.Add((roleUuid, resourceUuid, objectUuid));
        return Task.CompletedTask;
    }

    public Task<SubjectPermissionsDto> GetUserPermissionsAsync(Guid userUuid)
    {
        UserPermissionsRequested.Add(userUuid);
        return Task.FromResult(SubjectPermissions);
    }
}
