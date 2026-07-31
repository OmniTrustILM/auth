using Auth.Data;
using Auth.Data.Repositiories;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Data;

public class PermissionRepositoryTests : SqliteTestBase
{
    private Guid _roleUuid;
    private Guid _otherRoleUuid;
    private Guid _userUuid;
    private Guid _certificatesUuid;
    private Guid _groupsUuid;
    private Guid _listUuid;
    private readonly Guid _objectUuid = Guid.NewGuid();

    private async Task SeedWorld()
        => await Seed(context =>
        {
            var user = new User { Username = "jane" };
            var role = new Role { Name = "admin", Users = [user] };
            var otherRole = new Role { Name = "auditor", Users = [] };
            var list = new ActionEntity { Name = "list", DisplayName = "List" };
            var certificates = new Resource
            {
                Name = "certificates",
                DisplayName = "Certificates",
                ListObjectsEndpoint = "/v1/certificates",
                Actions = [list],
            };
            var groups = new Resource { Name = "groups", DisplayName = "Groups", Actions = [list] };

            context.AddRange(role, otherRole, certificates, groups);
            context.Permissions.AddRange(
                new Permission { Role = role, IsAllowed = true },
                new Permission { Role = role, Resource = certificates, Action = list, IsAllowed = true },
                new Permission { Role = role, Resource = certificates, Action = list, ObjectUuid = _objectUuid, ObjectName = "cert-1", IsAllowed = false },
                new Permission { Role = role, Resource = groups, Action = list, IsAllowed = true },
                new Permission { Role = otherRole, Resource = certificates, Action = list, IsAllowed = true });

            _roleUuid = role.Uuid;
            _otherRoleUuid = otherRole.Uuid;
            _userUuid = user.Uuid;
            _certificatesUuid = certificates.Uuid;
            _groupsUuid = groups.Uuid;
            _listUuid = list.Uuid;

            return Task.CompletedTask;
        });

    [Fact]
    public async Task GetRolePermissions_ReturnsEveryRowOfThatRoleWithItsResourceAndAction()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetRolePermissions(_roleUuid);

        Assert.Equal(4, permissions.Count);
        Assert.All(permissions, p => Assert.Equal(_roleUuid, p.RoleUuid));
        Assert.Contains(permissions, p => p.ResourceUuid == null && p.ActionUuid == null);
        Assert.All(permissions.Where(p => p.ResourceUuid != null), p => Assert.NotNull(p.Resource));
        Assert.All(permissions.Where(p => p.ActionUuid != null), p => Assert.NotNull(p.Action));
    }

    [Fact]
    public async Task GetRolePermissions_ReturnsNothingForARoleWithNoRows()
    {
        await SeedWorld();

        await using var context = NewContext();

        Assert.Empty(await new PermissionRepository(context).GetRolePermissions(Guid.NewGuid()));
    }

    [Fact]
    public async Task GetRoleResourcePermissions_NarrowsToOneResource()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetRoleResourcePermissions(_roleUuid, _certificatesUuid);

        Assert.Equal(2, permissions.Count);
        Assert.All(permissions, p => Assert.Equal(_certificatesUuid, p.ResourceUuid));
    }

    [Fact]
    public async Task GetRoleResourceObjectsPermissions_KeepsOnlyObjectScopedRows()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetRoleResourceObjectsPermissions(_roleUuid, _certificatesUuid);

        var permission = Assert.Single(permissions);
        Assert.Equal(_objectUuid, permission.ObjectUuid);
        Assert.Equal("cert-1", permission.ObjectName);
        Assert.False(permission.IsAllowed);
    }

    [Fact]
    public async Task GetUserPermissions_ReturnsTheRowsOfEveryRoleTheUserHolds()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetUserPermissions(_userUuid);

        Assert.Equal(4, permissions.Count);
        Assert.All(permissions, p => Assert.Equal(_roleUuid, p.RoleUuid));
    }

    [Fact]
    public async Task GetUserPermissions_LoadsTheRoleNarrowedToThatUser()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetUserPermissions(_userUuid);

        var role = Assert.Single(permissions.Select(p => p.Role).Distinct());
        Assert.Equal(_userUuid, Assert.Single(role.Users).Uuid);
    }

    [Fact]
    public async Task GetUserPermissions_ReturnsNothingForAUserWithNoRoles()
    {
        await SeedWorld();

        await using var context = NewContext();

        Assert.Empty(await new PermissionRepository(context).GetUserPermissions(Guid.NewGuid()));
    }

    [Fact]
    public async Task DeleteRolePermissionsWithoutObjects_LeavesTheObjectScopedRows()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRolePermissionsWithoutObjects(_roleUuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        var remaining = await reader.Permissions.Where(p => p.RoleUuid == _roleUuid).ToListAsync();
        Assert.Equal(_objectUuid, Assert.Single(remaining).ObjectUuid);
    }

    [Fact]
    public async Task DeleteRolePermissionsWithoutObjects_LeavesOtherRolesAlone()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRolePermissionsWithoutObjects(_roleUuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Single(await reader.Permissions.Where(p => p.RoleUuid == _otherRoleUuid).ToListAsync());
    }

    [Fact]
    public async Task DeleteRoleResourceObjectsPermissions_RemovesTheObjectRowsOfThatResourceOnly()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRoleResourceObjectsPermissions(_roleUuid, _certificatesUuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Empty(await reader.Permissions.Where(p => p.ObjectUuid != null).ToListAsync());
        Assert.Equal(3, await reader.Permissions.CountAsync(p => p.RoleUuid == _roleUuid));
    }

    [Fact]
    public async Task DeleteRoleResourceObjectsPermissions_DoesNothingForAResourceWithNoObjectRows()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRoleResourceObjectsPermissions(_roleUuid, _groupsUuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Equal(4, await reader.Permissions.CountAsync(p => p.RoleUuid == _roleUuid));
    }

    [Fact]
    public async Task DeleteRoleResourceObjectPermissions_RemovesTheRowsOfOneObject()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRoleResourceObjectPermissions(_roleUuid, _certificatesUuid, _objectUuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Empty(await reader.Permissions.Where(p => p.ObjectUuid == _objectUuid).ToListAsync());
    }

    [Fact]
    public async Task DeleteRoleResourceObjectPermissions_DoesNothingForAnUnknownObject()
    {
        await SeedWorld();

        await using var context = NewContext();
        new PermissionRepository(context).DeleteRoleResourceObjectPermissions(_roleUuid, _certificatesUuid, Guid.NewGuid());
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Equal(4, await reader.Permissions.CountAsync(p => p.RoleUuid == _roleUuid));
    }

    [Fact]
    public async Task ARowKeepsTheActionItPointsAt()
    {
        await SeedWorld();

        await using var context = NewContext();
        var permissions = await new PermissionRepository(context).GetRoleResourcePermissions(_roleUuid, _certificatesUuid);

        Assert.All(permissions, p => Assert.Equal(_listUuid, p.ActionUuid));
    }
}
