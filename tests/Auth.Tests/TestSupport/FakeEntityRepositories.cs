using Auth.Data.Contracts;
using Auth.Models.Entities;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.TestSupport;

public sealed class FakeUserRepository : FakeRepository<User>, IUserRepository
{
    public Task<IEnumerable<User>> GetRoleUsersAsync(Guid roleUuid)
        => Task.FromResult<IEnumerable<User>>(Stored.Where(u => u.Roles != null && u.Roles.Any(r => r.Uuid == roleUuid)).ToList());
}

public sealed class FakeRoleRepository : FakeRepository<Role>, IRoleRepository
{
    public Task<IEnumerable<Role>> GetUserRolesAsync(Guid userUuid)
        => Task.FromResult<IEnumerable<Role>>(Stored.Where(r => r.Users != null && r.Users.Any(u => u.Uuid == userUuid)).ToList());
}

public sealed class FakeResourceRepository : FakeRepository<Resource>, IResourceRepository
{
    public Task<List<Resource>> GetResourcesWithActions()
        => Task.FromResult(Stored.OrderBy(r => r.DisplayName).ToList());

    public Task<Dictionary<string, Resource>> GetResourcesWithActionsMap()
        => Task.FromResult(Stored.OrderBy(r => r.DisplayName).ToDictionary(r => r.Name));

    public Task<Dictionary<TKey, Resource>> GetResourcesMapAsync<TKey>(Func<Resource, TKey> keySelector) where TKey : notnull
        => Task.FromResult(Stored.ToDictionary(keySelector));
}

public sealed class FakeActionRepository : FakeRepository<ActionEntity>, IActionRepository
{
    public Task<ActionEntity?> GetActionByNameAsync(string actionName)
        => Task.FromResult(Stored.FirstOrDefault(a => string.Equals(a.Name, actionName, StringComparison.Ordinal)));

    public Task<Dictionary<TKey, ActionEntity>> GetActionsMapAsync<TKey>(Func<ActionEntity, TKey> keySelector) where TKey : notnull
        => Task.FromResult(Stored.ToDictionary(keySelector));
}

public sealed class FakePermissionRepository : FakeRepository<Permission>, IPermissionRepository
{
    public Task<List<Permission>> GetUserPermissions(Guid userUuid)
        => Task.FromResult(Ordered(Stored.Where(p => p.Role != null && p.Role.Users != null && p.Role.Users.Any(u => u.Uuid == userUuid))));

    public Task<List<Permission>> GetRolePermissions(Guid roleUuid)
        => Task.FromResult(Ordered(Stored.Where(p => p.RoleUuid == roleUuid)));

    public Task<List<Permission>> GetRoleResourcePermissions(Guid roleUuid, Guid resourceUuid)
        => Task.FromResult(Ordered(Stored.Where(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid)));

    public Task<List<Permission>> GetRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid)
        => Task.FromResult(Ordered(Stored.Where(p => p.ObjectUuid != null && p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid)));

    public void DeleteRolePermissionsWithoutObjects(Guid roleUuid)
        => DeleteMatching(p => p.RoleUuid == roleUuid && p.ObjectUuid == null);

    public void DeleteRoleResourceObjectsPermissions(Guid roleUuid, Guid resourceUuid)
        => DeleteMatching(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid && p.ObjectUuid != null);

    public void DeleteRoleResourceObjectPermissions(Guid roleUuid, Guid resourceUuid, Guid objectUuid)
        => DeleteMatching(p => p.RoleUuid == roleUuid && p.ResourceUuid == resourceUuid && p.ObjectUuid == objectUuid);

    private void DeleteMatching(Func<Permission, bool> predicate)
    {
        foreach (var permission in Stored.Where(predicate).ToList()) Delete(permission);
    }

    private static List<Permission> Ordered(IEnumerable<Permission> permissions)
        => permissions
            .OrderByDescending(p => p.ResourceUuid)
            .ThenByDescending(p => p.ActionUuid)
            .ThenByDescending(p => p.ObjectUuid)
            .ToList();
}
