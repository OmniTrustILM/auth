using Auth.Models.Entities;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.TestSupport;

/// <summary>
/// One role holding one user, plus whatever resources, actions and permission rows a test adds. Rows are stored with
/// their navigations already populated, the way the permission repository's includes deliver them.
/// </summary>
public sealed class PermissionWorld
{
    private readonly Dictionary<string, ActionEntity> _actions = new(StringComparer.Ordinal);

    public FakeRepositoryManager Manager { get; } = new();
    public Role Role { get; }
    public User User { get; }

    public PermissionWorld()
    {
        User = new User { Username = "jane", Enabled = true, Roles = [] };
        Role = new Role { Name = "admin", Users = [User], Permissions = [] };
        User.Roles.Add(Role);

        Manager.RoleRepository.Seed(Role);
        Manager.UserRepository.Seed(User);
    }

    public ActionEntity Action(string name)
    {
        if (_actions.TryGetValue(name, out var existing)) return existing;

        var action = new ActionEntity { Name = name, DisplayName = name };
        _actions.Add(name, action);
        Manager.ActionRepository.Seed(action);

        return action;
    }

    public Resource AddResource(string name, string? listObjectsEndpoint = null, params string[] actionNames)
    {
        var resource = new Resource
        {
            Name = name,
            DisplayName = name,
            ListObjectsEndpoint = listObjectsEndpoint,
            Actions = [.. actionNames.Select(Action)],
            Permissions = [],
        };
        Manager.ResourceRepository.Seed(resource);

        return resource;
    }

    public Permission AllowAllResources(bool isAllowed = true) => Add(new Permission { IsAllowed = isAllowed });

    public Permission AllResourceActions(Resource resource, bool isAllowed = true)
        => Add(new Permission { ResourceUuid = resource.Uuid, Resource = resource, IsAllowed = isAllowed });

    public Permission ResourceAction(Resource resource, string actionName, bool isAllowed = true)
    {
        var action = Action(actionName);

        return Add(new Permission
        {
            ResourceUuid = resource.Uuid,
            Resource = resource,
            ActionUuid = action.Uuid,
            Action = action,
            IsAllowed = isAllowed,
        });
    }

    public Permission ObjectAction(Resource resource, string actionName, Guid objectUuid, string? objectName, bool isAllowed)
    {
        var action = Action(actionName);

        return Add(new Permission
        {
            ResourceUuid = resource.Uuid,
            Resource = resource,
            ActionUuid = action.Uuid,
            Action = action,
            ObjectUuid = objectUuid,
            ObjectName = objectName,
            IsAllowed = isAllowed,
        });
    }

    private Permission Add(Permission permission)
    {
        permission.RoleUuid = Role.Uuid;
        permission.Role = Role;
        Manager.PermissionRepository.Seed(permission);

        return permission;
    }
}
