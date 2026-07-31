using Auth.Common.Data;
using Auth.Common.Exceptions;
using Auth.Data.Repositiories;
using Auth.Models.Entities;
using Microsoft.EntityFrameworkCore;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Data;

/// <summary>
/// The generic repository against a real provider, read through two concrete subclasses: users carry a detail include,
/// actions carry none, so both sides of the include composition are exercised.
/// </summary>
public class BaseRepositoryTests : SqliteTestBase
{
    private static ActionEntity Action(string name) => new() { Name = name, DisplayName = name.ToUpperInvariant() };

    private async Task SeedActions(params string[] names)
        => await Seed(context =>
        {
            context.Actions.AddRange(names.Select(Action));
            return Task.CompletedTask;
        });

    [Fact]
    public async Task FindAll_ReturnsEveryRowWithoutTracking()
    {
        await SeedActions("list", "detail");

        await using var context = NewContext();
        var actions = await new ActionRepository(context).FindAll().ToListAsync();

        Assert.Equal(2, actions.Count);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task FindByCondition_FiltersWithoutTracking()
    {
        await SeedActions("list", "detail");

        await using var context = NewContext();
        var actions = await new ActionRepository(context).FindByCondition(a => a.Name == "list").ToListAsync();

        Assert.Equal("list", Assert.Single(actions).Name);
        Assert.Empty(context.ChangeTracker.Entries());
    }

    [Fact]
    public async Task GetAllAsync_SortsAndPagesAndReportsTheFullCount()
    {
        await SeedActions("list", "detail", "create", "revoke");

        await using var context = NewContext();
        var page = await new ActionRepository(context).GetAllAsync(new QueryStringParameters
        {
            Page = 2,
            PageSize = 2,
            SortBy = nameof(ActionEntity.Name),
            SortAscending = true,
        });

        Assert.Equal(["list", "revoke"], page.Select(a => a.Name));
        Assert.Equal(4, page.TotalCount);
        Assert.Equal(2, page.TotalPages);
        Assert.True(page.HasPrevious);
        Assert.False(page.HasNext);
    }

    [Fact]
    public async Task GetAllAsync_SortsDescendingWhenAsked()
    {
        await SeedActions("list", "detail", "create");

        await using var context = NewContext();
        var page = await new ActionRepository(context).GetAllAsync(new QueryStringParameters
        {
            SortBy = nameof(ActionEntity.Name),
            SortAscending = false,
        });

        Assert.Equal(["list", "detail", "create"], page.Select(a => a.Name));
    }

    [Fact]
    public async Task GetAllAsync_LeavesRowsUnorderedWhenNoSortFieldIsGiven()
    {
        await SeedActions("list", "detail", "create");

        await using var context = NewContext();
        var page = await new ActionRepository(context).GetAllAsync(new QueryStringParameters { PageSize = 10 });

        Assert.Equal(3, page.Count);
        Assert.Equal(3, page.TotalCount);
    }

    [Fact]
    public async Task GetWhereAsync_PagesTheFilteredRows()
    {
        await SeedActions("list", "listObjects", "detail");

        await using var context = NewContext();
        var page = await new ActionRepository(context).GetWhereAsync(
            new QueryStringParameters { PageSize = 10, SortBy = nameof(ActionEntity.Name), SortAscending = true },
            a => a.Name.StartsWith("list"));

        Assert.Equal(["list", "listObjects"], page.Select(a => a.Name));
        Assert.Equal(2, page.TotalCount);
    }

    [Fact]
    public async Task GetByKeyAsync_ReturnsATrackedRow()
    {
        await SeedActions("list");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var uuid = (await repository.FindAll().SingleAsync()).Uuid;

        var action = await repository.GetByKeyAsync(uuid);

        Assert.Equal("list", action.Name);
        Assert.Equal(EntityState.Unchanged, context.Entry(action).State);
    }

    [Fact]
    public async Task GetByKeyAsync_LoadsTheDetailIncludesOfTheEntity()
    {
        await Seed(context =>
        {
            var role = new Role { Name = "admin" };
            context.Users.Add(new User { Username = "jane", Roles = [role] });
            return Task.CompletedTask;
        });

        await using var context = NewContext();
        var repository = new UserRepository(context);
        var uuid = (await repository.FindAll().SingleAsync()).Uuid;

        var user = await repository.GetByKeyAsync(uuid);

        Assert.Equal("admin", Assert.Single(user.Roles).Name);
    }

    [Fact]
    public async Task GetByKeyAsync_ReportsAMissingRowAsNotFound()
    {
        await using var context = NewContext();
        var uuid = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => new ActionRepository(context).GetByKeyAsync(uuid));

        Assert.Equal($"Cannot find entity Action with id {uuid}", exception.Message);
    }

    [Fact]
    public async Task GetByConditionAsync_ReturnsTheFirstMatchWithItsDetailIncludes()
    {
        await Seed(context =>
        {
            var role = new Role { Name = "admin" };
            context.Users.Add(new User { Username = "jane", Roles = [role] });
            return Task.CompletedTask;
        });

        await using var context = NewContext();
        var user = await new UserRepository(context).GetByConditionAsync(u => u.Username == "jane");

        Assert.NotNull(user);
        Assert.Equal("admin", Assert.Single(user.Roles).Name);
    }

    [Fact]
    public async Task GetByConditionAsync_ReturnsNullWhenNothingMatches()
    {
        await using var context = NewContext();

        Assert.Null(await new UserRepository(context).GetByConditionAsync(u => u.Username == "nobody"));
    }

    [Fact]
    public async Task GetByUuidsAsync_ReturnsTheRequestedSubset()
    {
        await SeedActions("list", "detail", "create");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var wanted = await repository.FindAll().Where(a => a.Name != "create").Select(a => a.Uuid).ToListAsync();

        var actions = await repository.GetByUuidsAsync(wanted);

        Assert.Equal(["detail", "list"], actions.Select(a => a.Name).Order());
    }

    [Fact]
    public async Task GetByUuidsAsync_ReturnsNothingForAnEmptyRequest()
    {
        await SeedActions("list");

        await using var context = NewContext();

        Assert.Empty(await new ActionRepository(context).GetByUuidsAsync([]));
    }

    [Fact]
    public async Task GetDictionaryMap_KeysEveryRowByTheSelector()
    {
        await SeedActions("list", "detail");

        await using var context = NewContext();
        var map = await new ActionRepository(context).GetDictionaryMap(a => a.Name);

        Assert.Equal(["detail", "list"], map.Keys.Order());
    }

    [Fact]
    public async Task GetDictionaryMap_IgnoresTheFilterItIsGiven()
    {
        // The filtered query is built and then discarded, so every row comes back regardless of the predicate. No
        // caller passes one today.
        await SeedActions("list", "detail");

        await using var context = NewContext();
        var map = await new ActionRepository(context).GetDictionaryMap(a => a.Name, a => a.Name == "list");

        Assert.Equal(2, map.Count);
    }

    [Fact]
    public async Task Create_PersistsOnceTheContextIsSaved()
    {
        await using var context = NewContext();
        new ActionRepository(context).Create(Action("list"));
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Equal("list", (await reader.Actions.SingleAsync()).Name);
    }

    [Fact]
    public async Task UpdateAsync_CopiesTheValuesOntoTheTrackedRowAndKeepsItsKey()
    {
        await SeedActions("list");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var uuid = (await repository.FindAll().SingleAsync()).Uuid;

        await repository.UpdateAsync(uuid, new ActionEntity { Name = "detail", DisplayName = "DETAIL" });
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        var stored = await reader.Actions.SingleAsync();
        Assert.Equal("detail", stored.Name);
        Assert.Equal(uuid, stored.Uuid);
    }

    [Fact]
    public async Task UpdateAsync_ReportsAMissingRowAsNotFound()
    {
        await using var context = NewContext();
        var uuid = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => new ActionRepository(context).UpdateAsync(uuid, Action("detail")));

        Assert.Equal($"Cannot update entity Action with id {uuid}", exception.Message);
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheRow()
    {
        await SeedActions("list");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var uuid = (await repository.FindAll().SingleAsync()).Uuid;

        await repository.DeleteAsync(uuid);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Empty(reader.Actions);
    }

    [Fact]
    public async Task DeleteAsync_ReportsAMissingRowAsNotFound()
    {
        await using var context = NewContext();
        var uuid = Guid.NewGuid();

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(() => new ActionRepository(context).DeleteAsync(uuid));

        Assert.Equal($"Cannot delete entity Action with id {uuid}", exception.Message);
    }

    [Fact]
    public async Task Delete_RemovesTheRowItIsHandedDirectly()
    {
        await SeedActions("list");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var action = await repository.GetByKeyAsync((await repository.FindAll().SingleAsync()).Uuid);

        repository.Delete(action);
        await context.SaveChangesAsync();

        await using var reader = NewContext();
        Assert.Empty(reader.Actions);
    }

    [Fact]
    public async Task Reload_DiscardsUnsavedChangesToTheEntity()
    {
        await SeedActions("list");

        await using var context = NewContext();
        var repository = new ActionRepository(context);
        var action = await repository.GetByKeyAsync((await repository.FindAll().SingleAsync()).Uuid);
        action.Name = "scribbled";

        repository.Reload(action);

        Assert.Equal("list", action.Name);
    }
}
