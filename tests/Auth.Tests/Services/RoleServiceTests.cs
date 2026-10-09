using Auth.Common.Exceptions;
using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Tests.TestSupport;

namespace Auth.Tests.Services;

public class RoleServiceTests
{
    private readonly FakeRepositoryManager _manager = new();
    private readonly FakePermissionService _permissions = new();

    private static Role Role(string name, bool systemRole = false) => new()
    {
        Name = name,
        SystemRole = systemRole,
        Users = [],
    };

    [Fact]
    public async Task CreateAsync_StoresTheRole()
    {
        var created = await ServiceFactory.Role(_manager, _permissions).CreateAsync(new RoleRequestDto { Name = "admin", Description = "administrators" });

        Assert.Equal("admin", created.Name);
        Assert.Equal("administrators", created.Description);
        Assert.Equal("admin", Assert.Single(_manager.RoleRepository.Stored).Name);
    }

    [Fact]
    public async Task CreateAsync_RejectsAnAlreadyTakenName()
    {
        _manager.RoleRepository.Seed(Role("admin"));

        var exception = await Assert.ThrowsAsync<EntityNotUniqueException>(
            () => ServiceFactory.Role(_manager, _permissions).CreateAsync(new RoleRequestDto { Name = "admin" }));

        Assert.Contains("admin", exception.Message);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsARequestOfTheWrongType()
    {
        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).CreateAsync(new UserRequestDto { Username = "jane" }));

        Assert.Equal("Cannot create role. Invalid DTO", exception.Message);
    }

    [Fact]
    public async Task CreateAsync_HandsRequestedPermissionsToThePermissionService()
    {
        var permissions = new RolePermissionsRequestDto { AllowAllResources = true };

        var created = await ServiceFactory.Role(_manager, _permissions).CreateAsync(new RoleRequestDto { Name = "admin", Permissions = permissions });

        var saved = Assert.Single(_permissions.SavedRolePermissions);
        Assert.Equal(created.Uuid, saved.RoleUuid);
        Assert.Same(permissions, saved.Permissions);
    }

    [Fact]
    public async Task CreateAsync_SkipsThePermissionServiceWhenNoPermissionsAreRequested()
    {
        await ServiceFactory.Role(_manager, _permissions).CreateAsync(new RoleRequestDto { Name = "admin" });

        Assert.Empty(_permissions.SavedRolePermissions);
    }

    [Fact]
    public async Task UpdateAsync_AppliesTheRequest()
    {
        var role = Role("admin");
        _manager.RoleRepository.Seed(role);

        var updated = await ServiceFactory.Role(_manager, _permissions).UpdateAsync(role.Uuid, new RoleUpdateRequestDto { Description = "changed" });

        Assert.Equal("changed", updated.Description);
        Assert.Equal("changed", role.Description);
    }

    [Fact]
    public async Task UpdateAsync_RefusesToTouchASystemRole()
    {
        var role = Role("superadmin", systemRole: true);
        _manager.RoleRepository.Seed(role);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).UpdateAsync(role.Uuid, new RoleUpdateRequestDto { Description = "changed" }));

        Assert.Equal("Cannot update system role.", exception.Message);
        Assert.Null(role.Description);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRole()
    {
        var role = Role("admin");
        _manager.RoleRepository.Seed(role);

        await ServiceFactory.Role(_manager, _permissions).DeleteAsync(role.Uuid);

        Assert.Empty(_manager.RoleRepository.Stored);
    }

    [Fact]
    public async Task DeleteAsync_RefusesToRemoveASystemRole()
    {
        var role = Role("superadmin", systemRole: true);
        _manager.RoleRepository.Seed(role);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(() => ServiceFactory.Role(_manager, _permissions).DeleteAsync(role.Uuid));

        Assert.Equal("Cannot delete system role.", exception.Message);
        Assert.Single(_manager.RoleRepository.Stored);
    }

    [Fact]
    public async Task GetUserRolesAsync_ReturnsOnlyTheRolesTheUserHolds()
    {
        var user = new User { Username = "jane" };
        var held = Role("admin");
        held.Users = [user];
        _manager.RoleRepository.Seed(held, Role("auditor"));

        var roles = await ServiceFactory.Role(_manager, _permissions).GetUserRolesAsync(user.Uuid);

        Assert.Equal("admin", Assert.Single(roles).Name);
    }

    [Fact]
    public async Task AssignUsersAsync_ReplacesTheWholeMembership()
    {
        var previous = new User { Username = "previous" };
        var role = Role("admin");
        role.Users = [previous];
        _manager.RoleRepository.Seed(role);

        var jane = new User { Username = "jane" };
        var john = new User { Username = "john" };
        _manager.UserRepository.Seed(previous, jane, john);

        var updated = await ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [jane.Uuid, john.Uuid]);

        Assert.Equal(["jane", "john"], updated.Users.Select(u => u.Username).Order());
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_ClearsTheMembershipWhenNoUsersAreGiven()
    {
        var role = Role("admin");
        role.Users = [new User { Username = "previous" }];
        _manager.RoleRepository.Seed(role);

        var updated = await ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, []);

        Assert.Empty(updated.Users);
    }

    private Role SeedPairedRole(out User systemUser)
    {
        systemUser = new User { Username = "acme", SystemUser = true };
        var role = Role("acme", systemRole: true);
        role.Users = [systemUser];
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(systemUser);
        return role;
    }

    [Fact]
    public async Task AssignUsersAsync_PairsANewSystemUserWithItsNewRole()
    {
        var role = Role("acme", systemRole: true);
        var systemUser = new User { Username = "acme", SystemUser = true, Roles = [] };
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(systemUser);

        var updated = await ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid]);

        Assert.Equal("acme", Assert.Single(updated.Users).Username);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesAHumanAlongsideAFirstPairing()
    {
        var role = Role("acme", systemRole: true);
        var systemUser = new User { Username = "acme", SystemUser = true, Roles = [] };
        var jane = new User { Username = "jane" };
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(systemUser, jane);

        await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid, jane.Uuid]));

        Assert.Empty(role.Users);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesTwoSystemUsersInAFirstPairing()
    {
        var role = Role("acme", systemRole: true);
        var acme = new User { Username = "acme", SystemUser = true, Roles = [] };
        var scep = new User { Username = "scep", SystemUser = true, Roles = [] };
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(acme, scep);

        await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [acme.Uuid, scep.Uuid]));

        Assert.Empty(role.Users);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesASystemUserThatAlreadyHoldsARoleOnAnEmptySystemRole()
    {
        var role = Role("other", systemRole: true);
        var systemUser = new User { Username = "acme", SystemUser = true, Roles = [Role("acme", systemRole: true)] };
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(systemUser);

        await Assert.ThrowsAsync<InvalidActionException>(() => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid]));

        Assert.Empty(role.Users);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesAHumanOnARolePairedWithASystemUser()
    {
        var role = SeedPairedRole(out var systemUser);
        var jane = new User { Username = "jane" };
        _manager.UserRepository.Seed(jane);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid, jane.Uuid]));

        Assert.Equal("Role 'acme' belongs to a system user and cannot be assigned to user 'jane'.", exception.Message);
        Assert.Equal([systemUser], role.Users);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesToDetachTheSystemUserFromItsRole()
    {
        var role = SeedPairedRole(out _);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, []));

        Assert.Equal("Role 'acme' belongs to system user 'acme', which cannot be removed from it.", exception.Message);
        Assert.Single(role.Users);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_RefusesASystemUserOnAnotherRole()
    {
        var role = Role("admin");
        var systemUser = new User { Username = "acme", SystemUser = true };
        _manager.RoleRepository.Seed(role);
        _manager.UserRepository.Seed(systemUser);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid]));

        Assert.Equal("System user 'acme' holds only its own role and cannot be added to role 'admin'.", exception.Message);
        Assert.Empty(role.Users);
    }

    [Fact]
    public async Task AssignUsersAsync_AllowsAReplacementThatKeepsTheSystemUser()
    {
        var role = SeedPairedRole(out var systemUser);

        var updated = await ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid]);

        Assert.Equal("acme", Assert.Single(updated.Users).Username);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_AllowsAReplacementThatKeepsAnExistingHumanMember()
    {
        var role = SeedPairedRole(out var systemUser);
        var jane = new User { Username = "jane" };
        role.Users.Add(jane);
        _manager.UserRepository.Seed(jane);

        var updated = await ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(role.Uuid, [systemUser.Uuid, jane.Uuid]);

        Assert.Equal(["acme", "jane"], updated.Users.Select(u => u.Username).Order());
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignUsersAsync_ReportsAnUnknownRoleAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => ServiceFactory.Role(_manager, _permissions).AssignUsersAsync(Guid.NewGuid(), []));
    }
}
