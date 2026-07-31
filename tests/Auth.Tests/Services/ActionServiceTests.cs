using Auth.Common.Exceptions;
using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Tests.TestSupport;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Services;

/// <summary>
/// <c>ActionService</c> adds nothing to <c>CrudService</c>, so these cover the generic CRUD base through its simplest
/// concrete subclass.
/// </summary>
public class ActionServiceTests
{
    private readonly FakeRepositoryManager _manager = new();

    private static ActionEntity Action(string name) => new() { Name = name, DisplayName = name.ToUpperInvariant() };

    [Fact]
    public async Task GetAsync_ReturnsThePageWithItsMetadata()
    {
        _manager.ActionRepository.Seed(Action("list"), Action("detail"), Action("create"));

        var page = await ServiceFactory.Action(_manager).GetAsync(new QueryRequestDto { Page = 1, PageSize = 2, SortBy = "name" });

        Assert.Equal(["create", "detail"], page.Data.Select(a => a.Name));
        Assert.Equal(3, page.Links.TotalCount);
        Assert.Equal(2, page.Links.TotalPages);
        Assert.True(page.Links.HasNext);
        Assert.False(page.Links.HasPrevious);
    }

    [Fact]
    public async Task GetAsync_HonoursTheDescendingSortPrefix()
    {
        _manager.ActionRepository.Seed(Action("list"), Action("detail"), Action("create"));

        var page = await ServiceFactory.Action(_manager).GetAsync(new QueryRequestDto { SortBy = "-name" });

        Assert.Equal(["list", "detail", "create"], page.Data.Select(a => a.Name));
    }

    [Fact]
    public async Task GetAsync_ReturnsTheSecondPage()
    {
        _manager.ActionRepository.Seed(Action("list"), Action("detail"), Action("create"));

        var page = await ServiceFactory.Action(_manager).GetAsync(new QueryRequestDto { Page = 2, PageSize = 2, SortBy = "name" });

        Assert.Equal(["list"], page.Data.Select(a => a.Name));
        Assert.True(page.Links.HasPrevious);
        Assert.False(page.Links.HasNext);
    }

    [Fact]
    public async Task GetAsync_ReturnsAnEmptyPageWhenNothingIsStored()
    {
        var page = await ServiceFactory.Action(_manager).GetAsync(new QueryRequestDto());

        Assert.Empty(page.Data);
        Assert.Equal(0, page.Links.TotalCount);
    }

    [Fact]
    public async Task CreateAsync_PersistsTheEntityAndReturnsItsDetail()
    {
        var created = await ServiceFactory.Action(_manager).CreateAsync(new ActionRequestDto { Name = "list", DisplayName = "List" });

        Assert.Equal("list", created.Name);
        Assert.Equal("List", created.DisplayName);
        Assert.NotEqual(Guid.Empty, created.Uuid);
        Assert.Equal(1, _manager.SaveCount);

        var stored = Assert.Single(_manager.ActionRepository.Stored);
        Assert.Equal(created.Uuid, stored.Uuid);
    }

    [Fact]
    public async Task CreateAsync_RejectsARequestTheMapperCannotRead()
    {
        await Assert.ThrowsAsync<ArgumentException>(
            () => ServiceFactory.Action(_manager).CreateAsync(new ResourceRequestDto { Name = "certificates" }));

        Assert.Equal(0, _manager.SaveCount);
    }

    [Fact]
    public async Task GetDetailAsync_ReturnsTheStoredEntity()
    {
        var action = Action("list");
        _manager.ActionRepository.Seed(action);

        Assert.Equal("list", (await ServiceFactory.Action(_manager).GetDetailAsync(action.Uuid)).Name);
    }

    [Fact]
    public async Task GetDetailAsync_ReportsAnUnknownKeyAsNotFound()
    {
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Action(_manager).GetDetailAsync(Guid.NewGuid()));

        Assert.Equal("ENTITY_NOT_FOUND", exception.Code);
    }

    [Fact]
    public async Task UpdateAsync_AppliesTheRequestToTheStoredEntityAndSaves()
    {
        var action = Action("list");
        _manager.ActionRepository.Seed(action);

        var updated = await ServiceFactory.Action(_manager).UpdateAsync(action.Uuid, new ActionRequestDto { Name = "detail", DisplayName = "Detail" });

        Assert.Equal("detail", updated.Name);
        Assert.Equal("detail", action.Name);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task UpdateAsync_ReportsAnUnknownKeyAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Action(_manager).UpdateAsync(Guid.NewGuid(), new ActionRequestDto { Name = "detail" }));
    }

    [Fact]
    public async Task DeleteAsync_RemovesTheEntityAndSaves()
    {
        var action = Action("list");
        _manager.ActionRepository.Seed(action);

        await ServiceFactory.Action(_manager).DeleteAsync(action.Uuid);

        Assert.Empty(_manager.ActionRepository.Stored);
        Assert.Equal(1, _manager.SaveCount);
    }

    [Fact]
    public async Task DeleteAsync_ReportsAnUnknownKeyAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(() => ServiceFactory.Action(_manager).DeleteAsync(Guid.NewGuid()));
    }
}
