using Auth.Data.Repositiories;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace Auth.Tests.Data;

public class RepositoryManagerTests : SqliteTestBase
{
    [Fact]
    public void EachRepositoryIsCreatedOnceAndReused()
    {
        using var context = NewContext();
        var manager = new RepositoryManager(context);

        Assert.Same(manager.User, manager.User);
        Assert.Same(manager.Role, manager.Role);
        Assert.Same(manager.Permission, manager.Permission);
        Assert.Same(manager.Resource, manager.Resource);
        Assert.Same(manager.Action, manager.Action);
    }

    [Fact]
    public async Task SaveAsync_CommitsWhatTheRepositoriesStaged()
    {
        await using var context = NewContext();
        var manager = new RepositoryManager(context);
        manager.Role.Create(new Role { Name = "admin" });

        await manager.SaveAsync();

        await using var reader = NewContext();
        Assert.Equal("admin", (await reader.Roles.SingleAsync()).Name);
    }

    [Fact]
    public async Task ATransactionKeepsItsWritesWhenCommitted()
    {
        await using var context = NewContext();
        var manager = new RepositoryManager(context);

        await using (var transaction = await manager.BeginTransactionAsync())
        {
            manager.Role.Create(new Role { Name = "admin" });
            await manager.SaveAsync();
            await transaction.CommitAsync();
        }

        await using var reader = NewContext();
        Assert.Single(reader.Roles);
    }

    [Fact]
    public async Task ATransactionDiscardsItsWritesWhenRolledBack()
    {
        await using var context = NewContext();
        var manager = new RepositoryManager(context);

        await using (var transaction = await manager.BeginTransactionAsync())
        {
            manager.Role.Create(new Role { Name = "admin" });
            await manager.SaveAsync();
            await transaction.RollbackAsync();
        }

        await using var reader = NewContext();
        Assert.Empty(reader.Roles);
    }

    [Fact]
    public async Task Detach_StopsTrackingTheEntity()
    {
        await using var context = NewContext();
        var manager = new RepositoryManager(context);
        var role = new Role { Name = "admin" };
        manager.Role.Create(role);

        manager.Detach(role);
        await manager.SaveAsync();

        await using var reader = NewContext();
        Assert.Empty(reader.Roles);
        Assert.Equal(EntityState.Detached, context.Entry(role).State);
    }
}
