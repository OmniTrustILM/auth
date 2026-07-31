using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Data;

public class AuthDbContextTests : SqliteTestBase
{
    [Fact]
    public void Model_MapsEveryEntitySet()
    {
        using var context = NewContext();

        Assert.NotNull(context.Model.FindEntityType(typeof(User)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Role)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Permission)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Resource)));
        Assert.NotNull(context.Model.FindEntityType(typeof(Auth.Models.Entities.Action)));
    }

    [Fact]
    public async Task SaveChangesAsync_StampsCreatedAndUpdated_OnInsert()
    {
        var before = DateTimeOffset.UtcNow.AddSeconds(-1);

        await using var context = NewContext();
        var role = new Role { Name = "stamped" };
        context.Roles.Add(role);
        await context.SaveChangesAsync();

        Assert.InRange(role.CreatedAt, before, DateTimeOffset.UtcNow.AddSeconds(1));
        Assert.InRange(role.UpdatedAt, before, DateTimeOffset.UtcNow.AddSeconds(1));
    }

    [Fact]
    public async Task SaveChangesAsync_MovesUpdatedOnly_OnModify()
    {
        Guid roleUuid;
        DateTimeOffset createdAt;
        DateTimeOffset firstUpdatedAt;

        await using (var context = NewContext())
        {
            var role = new Role { Name = "modified" };
            context.Roles.Add(role);
            await context.SaveChangesAsync();

            roleUuid = role.Uuid;
            createdAt = role.CreatedAt;
            firstUpdatedAt = role.UpdatedAt;
        }

        await using (var context = NewContext())
        {
            var role = await context.Roles.SingleAsync(r => r.Uuid == roleUuid);
            role.Description = "changed";
            await context.SaveChangesAsync();

            Assert.Equal(createdAt, role.CreatedAt);
            Assert.True(role.UpdatedAt >= firstUpdatedAt);
        }
    }

    [Fact]
    public void SaveChanges_StampsSynchronously()
    {
        using var context = NewContext();
        var role = new Role { Name = "sync-stamped" };
        context.Roles.Add(role);
        context.SaveChanges();

        Assert.NotEqual(default, role.CreatedAt);
        Assert.NotEqual(default, role.UpdatedAt);
    }

    [Fact]
    public async Task AnActionCanBeReadBackWithTheResourcesAndPermissionsThatReferenceIt()
    {
        Guid actionUuid;

        await using (var context = NewContext())
        {
            var action = new Auth.Models.Entities.Action { Name = "revoke", DisplayName = "Revoke" };
            var resource = new Resource
            {
                Name = "certificates",
                DisplayName = "Certificates",
                ListObjectsEndpoint = "/v1/certificates",
                Actions = [action],
            };
            var role = new Role { Name = "admin" };
            context.AddRange(resource, role);
            context.Permissions.Add(new Permission { Role = role, Resource = resource, Action = action, IsAllowed = true });
            await context.SaveChangesAsync();

            actionUuid = action.Uuid;
        }

        await using var reader = NewContext();
        var stored = await reader.Actions
            .Include(a => a.Resources)
            .Include(a => a.Permissions)
            .SingleAsync(a => a.Uuid == actionUuid);

        Assert.Equal("certificates", Assert.Single(stored.Resources).Name);
        Assert.True(Assert.Single(stored.Permissions).IsAllowed);
    }

    [Fact]
    public async Task SaveChangesAsync_LeavesNonTimestampedEntitiesAlone()
    {
        await using var context = NewContext();
        var action = new Auth.Models.Entities.Action { Name = "detail", DisplayName = "Detail" };
        context.Actions.Add(action);

        await context.SaveChangesAsync();

        Assert.Equal(1, await context.Actions.CountAsync());
    }
}
