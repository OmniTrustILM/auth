using Auth.Data.Repositiories;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Data;

public class EntityRepositoryTests : SqliteTestBase
{
    [Fact]
    public async Task UserRepository_ReturnsTheHoldersOfARoleWithTheirRoles()
    {
        Guid roleUuid = default;
        await Seed(context =>
        {
            var admin = new Role { Name = "admin" };
            var auditor = new Role { Name = "auditor" };
            context.Users.Add(new User { Username = "jane", Roles = [admin] });
            context.Users.Add(new User { Username = "john", Roles = [auditor] });
            context.ChangeTracker.DetectChanges();
            roleUuid = admin.Uuid;

            return Task.CompletedTask;
        });

        await using var context = NewContext();
        var users = (await new UserRepository(context).GetRoleUsersAsync(roleUuid)).ToList();

        var user = Assert.Single(users);
        Assert.Equal("jane", user.Username);
        Assert.Equal("admin", Assert.Single(user.Roles).Name);
    }

    [Fact]
    public async Task UserRepository_ReturnsNothingForARoleNobodyHolds()
    {
        await using var context = NewContext();

        Assert.Empty(await new UserRepository(context).GetRoleUsersAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task RoleRepository_ReturnsTheRolesOfAUserWithTheirMembers()
    {
        Guid userUuid = default;
        await Seed(context =>
        {
            var jane = new User { Username = "jane" };
            context.Roles.Add(new Role { Name = "admin", Users = [jane] });
            context.Roles.Add(new Role { Name = "auditor", Users = [new User { Username = "john" }] });
            userUuid = jane.Uuid;

            return Task.CompletedTask;
        });

        await using var context = NewContext();
        var roles = (await new RoleRepository(context).GetUserRolesAsync(userUuid)).ToList();

        var role = Assert.Single(roles);
        Assert.Equal("admin", role.Name);
        Assert.Equal("jane", Assert.Single(role.Users).Username);
    }

    [Fact]
    public async Task RoleRepository_ReturnsNothingForAUserWithNoRoles()
    {
        await using var context = NewContext();

        Assert.Empty(await new RoleRepository(context).GetUserRolesAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ResourceRepository_ReturnsResourcesWithActionsOrderedByDisplayName()
    {
        await SeedResources();

        await using var context = NewContext();
        var resources = await new ResourceRepository(context).GetResourcesWithActions();

        Assert.Equal(["Certificates", "Groups"], resources.Select(r => r.DisplayName));
        Assert.Equal(["detail", "list"], resources[0].Actions.Select(a => a.Name).Order());
    }

    [Fact]
    public async Task ResourceRepository_KeysTheResourceMapByName()
    {
        await SeedResources();

        await using var context = NewContext();
        var map = await new ResourceRepository(context).GetResourcesWithActionsMap();

        Assert.Equal(["certificates", "groups"], map.Keys.Order());
        Assert.Equal(2, map["certificates"].Actions.Count);
    }

    [Fact]
    public async Task ResourceRepository_TracksTheRowsItReturnsForWriting()
    {
        await SeedResources();

        await using var context = NewContext();
        var map = await new ResourceRepository(context).GetResourcesMapAsync(r => r.Name);

        Assert.Equal(["certificates", "groups"], map.Keys.Order());
        Assert.All(map.Values, resource => Assert.Equal(EntityState.Unchanged, context.Entry(resource).State));
    }

    [Fact]
    public async Task ActionRepository_KeysTheActionMapByTheSelector()
    {
        await SeedResources();

        await using var context = NewContext();
        var map = await new ActionRepository(context).GetActionsMapAsync(a => a.Name);

        Assert.Equal(["detail", "list"], map.Keys.Order());
        Assert.All(map.Values, action => Assert.Equal(EntityState.Unchanged, context.Entry(action).State));
    }

    [Fact]
    public async Task ActionRepository_CannotLookUpAnActionByName()
    {
        // The lookup compares with an explicit StringComparison, which the provider cannot translate. Nothing calls it.
        await SeedResources();

        await using var context = NewContext();

        await Assert.ThrowsAsync<InvalidOperationException>(() => new ActionRepository(context).GetActionByNameAsync("list"));
    }

    private async Task SeedResources()
        => await Seed(context =>
        {
            var list = new ActionEntity { Name = "list", DisplayName = "List" };
            var detail = new ActionEntity { Name = "detail", DisplayName = "Detail" };
            context.Resources.Add(new Resource
            {
                Name = "certificates",
                DisplayName = "Certificates",
                ListObjectsEndpoint = "/v1/certificates",
                Actions = [list, detail],
            });
            context.Resources.Add(new Resource { Name = "groups", DisplayName = "Groups", Actions = [list] });

            return Task.CompletedTask;
        });
}
