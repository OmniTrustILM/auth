using Auth.Common.Exceptions;
using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Tests.TestSupport;

namespace Auth.Tests.Services;

public class UserServiceTests
{
    private readonly FakeRepositoryManager _manager = new();

    private static User User(string username, bool systemUser = false, List<NameAndUuidDto>? groups = null) => new()
    {
        Username = username,
        Enabled = true,
        SystemUser = systemUser,
        Groups = groups,
        Roles = [],
    };

    private static NameAndUuidDto Group(string name) => new() { Uuid = Guid.NewGuid(), Name = name };

    [Fact]
    public async Task GetAsync_ReturnsEveryUserWhenNoGroupIsRequested()
    {
        _manager.UserRepository.Seed(User("jane"), User("john"));

        var page = await ServiceFactory.User(_manager).GetAsync(new UserQueryRequestDto { SortBy = "username" });

        Assert.Equal(["jane", "john"], page.Data.Select(u => u.Username));
        Assert.Equal(2, page.Links.TotalCount);
    }

    [Fact]
    public async Task GetAsync_KeepsOnlyMembersOfTheRequestedGroup()
    {
        _manager.UserRepository.Seed(
            User("jane", groups: [Group("operators")]),
            User("john", groups: [Group("auditors")]),
            User("jack", groups: [Group("auditors"), Group("operators")]));

        var page = await ServiceFactory.User(_manager).GetAsync(new UserQueryRequestDto { Group = "operators", SortBy = "username" });

        Assert.Equal(["jack", "jane"], page.Data.Select(u => u.Username).Order());
        Assert.Equal(2, page.Links.TotalCount);
    }

    [Fact]
    public async Task GetAsync_ExcludesUsersWithNoGroupsAtAll()
    {
        _manager.UserRepository.Seed(User("jane", groups: null), User("john", groups: []));

        var page = await ServiceFactory.User(_manager).GetAsync(new UserQueryRequestDto { Group = "operators" });

        Assert.Empty(page.Data);
        Assert.Equal(0, page.Links.TotalCount);
    }

    [Fact]
    public async Task GetAsync_PagesTheWholeFilteredResult()
    {
        _manager.UserRepository.Seed(
            User("jack", groups: [Group("operators")]),
            User("jane", groups: [Group("operators")]),
            User("john", groups: [Group("operators")]));

        var page = await ServiceFactory.User(_manager)
            .GetAsync(new UserQueryRequestDto { Group = "operators", Page = 2, PageSize = 2, SortBy = "username" });

        Assert.Equal(["john"], page.Data.Select(u => u.Username));
        Assert.Equal(3, page.Links.TotalCount);
        Assert.Equal(2, page.Links.TotalPages);
        Assert.True(page.Links.HasPrevious);
        Assert.False(page.Links.HasNext);
    }

    [Fact]
    public async Task GetAsync_CountsOnlyGroupMembers_NotEveryUser()
    {
        _manager.UserRepository.Seed(
            User("jane", groups: [Group("operators")]),
            User("john", groups: [Group("auditors")]),
            User("jack", groups: null));

        var page = await ServiceFactory.User(_manager).GetAsync(new UserQueryRequestDto { Group = "operators" });

        Assert.Equal(["jane"], page.Data.Select(u => u.Username));
        Assert.Equal(1, page.Links.TotalCount);
    }

    [Fact]
    public async Task GetAsync_AppliesNoGroupFilterForAPlainQueryRequest()
    {
        _manager.UserRepository.Seed(User("jane", groups: null));

        var page = await ServiceFactory.User(_manager).GetAsync(new QueryRequestDto());

        Assert.Single(page.Data);
    }

    [Fact]
    public async Task CreateAsync_StoresTheUser()
    {
        var created = await ServiceFactory.User(_manager).CreateAsync(new UserRequestDto { Username = "jane", Enabled = true });

        Assert.Equal("jane", created.Username);
        Assert.True(created.Enabled);
        Assert.Equal("jane", Assert.Single(_manager.UserRepository.Stored).Username);
    }

    [Fact]
    public async Task CreateAsync_RejectsAnAlreadyTakenUsername()
    {
        _manager.UserRepository.Seed(User("jane"));

        var exception = await Assert.ThrowsAsync<EntityNotUniqueException>(
            () => ServiceFactory.User(_manager).CreateAsync(new UserRequestDto { Username = "jane" }));

        Assert.Contains("jane", exception.Message);
        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task CreateAsync_RejectsARequestOfTheWrongType()
    {
        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.User(_manager).CreateAsync(new RoleRequestDto { Name = "admin" }));

        Assert.Equal("Cannot create user. Invalid DTO", exception.Message);
    }

    [Fact]
    public async Task UpdateAsync_AppliesTheRequest()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);

        var updated = await ServiceFactory.User(_manager).UpdateAsync(user.Uuid, new UserUpdateRequestDto { FirstName = "Jane" });

        Assert.Equal("Jane", updated.FirstName);
        Assert.Equal("Jane", user.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_RefusesToTouchASystemUser()
    {
        var user = User("system", systemUser: true);
        _manager.UserRepository.Seed(user);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.User(_manager).UpdateAsync(user.Uuid, new UserUpdateRequestDto { FirstName = "Jane" }));

        Assert.Equal("Cannot update system user.", exception.Message);
        Assert.Null(user.FirstName);
    }

    [Fact]
    public async Task UpdateAsync_ReportsAnUnknownUserAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.User(_manager).UpdateAsync(Guid.NewGuid(), new UserUpdateRequestDto()));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheUser()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);

        await ServiceFactory.User(_manager).DeleteAsync(user.Uuid);

        Assert.Empty(_manager.UserRepository.Stored);
    }

    [Fact]
    public async Task DeleteAsync_RefusesToRemoveASystemUser()
    {
        var user = User("system", systemUser: true);
        _manager.UserRepository.Seed(user);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(() => ServiceFactory.User(_manager).DeleteAsync(user.Uuid));

        Assert.Equal("Cannot delete system user.", exception.Message);
        Assert.Single(_manager.UserRepository.Stored);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public async Task EnableUserAsync_SetsTheFlagAndSaves(bool enable)
    {
        var user = User("jane");
        user.Enabled = !enable;
        _manager.UserRepository.Seed(user);

        var updated = await ServiceFactory.User(_manager).EnableUserAsync(user.Uuid, enable);

        Assert.Equal(enable, updated.Enabled);
        Assert.Equal(enable, user.Enabled);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task EnableUserAsync_ReportsAnUnknownUserAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => ServiceFactory.User(_manager).EnableUserAsync(Guid.NewGuid(), true));
    }

    [Fact]
    public async Task AssignRoleAsync_AddsTheRoleToTheUser()
    {
        var user = User("jane");
        var role = new Role { Name = "admin", Users = [] };
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(role);

        var updated = await ServiceFactory.User(_manager).AssignRoleAsync(user.Uuid, role.Uuid);

        Assert.Equal("admin", Assert.Single(updated.Roles).Name);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task AssignRoleAsync_ReportsAnUnknownRoleAsNotFound()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);

        await Assert.ThrowsAsync<EntityNotFoundException>(() => ServiceFactory.User(_manager).AssignRoleAsync(user.Uuid, Guid.NewGuid()));
    }

    [Fact]
    public async Task AssignRolesAsync_ReplacesTheWholeRoleSet()
    {
        var previous = new Role { Name = "previous", Users = [] };
        var user = User("jane");
        user.Roles.Add(previous);
        _manager.UserRepository.Seed(user);

        var admin = new Role { Name = "admin", Users = [] };
        var auditor = new Role { Name = "auditor", Users = [] };
        _manager.RoleRepository.Seed(previous, admin, auditor);

        var updated = await ServiceFactory.User(_manager).AssignRolesAsync(user.Uuid, [admin.Uuid, auditor.Uuid]);

        Assert.Equal(["admin", "auditor"], updated.Roles.Select(r => r.Name).Order());
    }

    [Fact]
    public async Task AssignRolesAsync_ClearsTheRoleSetWhenNoRoleIsGiven()
    {
        var user = User("jane");
        user.Roles.Add(new Role { Name = "previous", Users = [] });
        _manager.UserRepository.Seed(user);

        Assert.Empty((await ServiceFactory.User(_manager).AssignRolesAsync(user.Uuid, [])).Roles);
    }

    [Fact]
    public async Task RemoveRoleAsync_TakesTheRoleAway()
    {
        var role = new Role { Name = "admin", Users = [] };
        var kept = new Role { Name = "auditor", Users = [] };
        var user = User("jane");
        user.Roles.Add(role);
        user.Roles.Add(kept);
        _manager.UserRepository.Seed(user);
        _manager.RoleRepository.Seed(role, kept);

        var updated = await ServiceFactory.User(_manager).RemoveRoleAsync(user.Uuid, role.Uuid);

        Assert.Equal("auditor", Assert.Single(updated.Roles).Name);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task GetRoleUsersAsync_ReturnsOnlyTheHoldersOfThatRole()
    {
        var role = new Role { Name = "admin", Users = [] };
        var holder = User("jane");
        holder.Roles.Add(role);
        _manager.UserRepository.Seed(holder, User("john"));
        _manager.RoleRepository.Seed(role);

        var users = await ServiceFactory.User(_manager).GetRoleUsersAsync(role.Uuid);

        Assert.Equal("jane", Assert.Single(users).Username);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsTheStoredUser()
    {
        var user = User("jane");
        _manager.UserRepository.Seed(user);

        Assert.Equal("jane", (await ServiceFactory.User(_manager).GetDetailAsync(user.Uuid)).Username);
    }
}
