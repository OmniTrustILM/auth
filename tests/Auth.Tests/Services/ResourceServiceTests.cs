using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Tests.TestSupport;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Services;

public class ResourceServiceTests
{
    private readonly FakeRepositoryManager _manager = new();

    private static Resource Resource(string name, string displayName, string? listObjectsEndpoint = null, ActionEntity[]? actions = null) => new()
    {
        Name = name,
        DisplayName = displayName,
        ListObjectsEndpoint = listObjectsEndpoint,
        Actions = [.. actions ?? []],
    };

    private static ActionEntity Action(string name) => new() { Name = name, DisplayName = name };

    private static ResourceSyncRequestDto Sync(string name, string? listObjectsEndpoint, params string[] actions)
        => new() { Name = name, ListObjectsEndpoint = listObjectsEndpoint, Actions = [.. actions] };

    [Fact]
    public async Task GetAllResourcesAsync_ReturnsResourcesWithTheirActionsOrderedByDisplayName()
    {
        _manager.ResourceRepository.Seed(
            Resource("groups", "Groups", actions: [Action("list")]),
            Resource("certificates", "Certificates", actions: [Action("detail"), Action("list")]));

        var resources = await ServiceFactory.Resource(_manager).GetAllResourcesAsync();

        Assert.Equal(["Certificates", "Groups"], resources.Select(r => r.DisplayName));
        Assert.Equal(["detail", "list"], resources[0].Actions.Select(a => a.Name));
    }

    [Fact]
    public async Task AddResourcesAsync_CreatesTheResourceWithADerivedDisplayName()
    {
        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("raProfile", "/v1/ra-profiles", "list")]);

        var stored = Assert.Single(_manager.ResourceRepository.Stored);
        Assert.Equal("raProfile", stored.Name);
        Assert.Equal("Ra Profile", stored.DisplayName);
        Assert.Equal("/v1/ra-profiles", stored.ListObjectsEndpoint);
        Assert.Equal(1, _manager.ResourceRepository.ReloadCount);
        Assert.True(_manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task AddResourcesAsync_CreatesTheActionsTheResourceNeeds()
    {
        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", null, "list", "issue")]);

        Assert.Equal(["issue", "list"], _manager.ActionRepository.Stored.Select(a => a.Name).Order());
        Assert.Equal(["issue", "list"], Assert.Single(_manager.ResourceRepository.Stored).Actions.Select(a => a.Name).Order());
    }

    [Fact]
    public async Task AddResourcesAsync_DerivesDisplayNamesForNewActions()
    {
        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", null, "listObjects")]);

        Assert.Equal("List Objects", Assert.Single(_manager.ActionRepository.Stored).DisplayName);
    }

    [Fact]
    public async Task AddResourcesAsync_SkipsTheWildcardActionName()
    {
        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", null, "ANY", "list")]);

        Assert.Equal("list", Assert.Single(_manager.ActionRepository.Stored).Name);
    }

    [Fact]
    public async Task AddResourcesAsync_ReusesAnExistingActionRatherThanCreatingASecond()
    {
        _manager.ActionRepository.Seed(Action("list"));

        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", null, "list")]);

        Assert.Single(_manager.ActionRepository.Stored);
    }

    [Fact]
    public async Task AddResourcesAsync_LeavesAnAlreadyKnownResourceInPlace()
    {
        var existing = Resource("certificates", "Certificates", "/v1/certificates");
        _manager.ResourceRepository.Seed(existing);
        _manager.ActionRepository.Seed(Action("list"));

        await ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", "/v2/certificates", "list")]);

        Assert.Single(_manager.ResourceRepository.Stored);
        Assert.Equal("/v1/certificates", existing.ListObjectsEndpoint);
        Assert.Equal(0, _manager.ResourceRepository.ReloadCount);
    }

    [Fact]
    public async Task AddResourcesAsync_RollsBackWhenTheSaveFails()
    {
        _manager.ActionRepository.Seed(Action("list"));
        _manager.OnSave = _ => new InvalidOperationException("deadlock");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServiceFactory.Resource(_manager).AddResourcesAsync([Sync("certificates", null, "list")]));

        var transaction = _manager.SingleTransaction();
        Assert.True(transaction.RolledBack);
        Assert.False(transaction.Committed);
        Assert.Empty(_manager.ResourceRepository.Stored);
    }

    [Fact]
    public async Task SyncResourcesAsync_ReportsANewResourceAndItsNewActionsAsAdded()
    {
        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", "/v1/certificates", "list")]);

        Assert.Equal(["certificates"], result.Resources.Added);
        Assert.Empty(result.Resources.Updated);
        Assert.Empty(result.Resources.Removed);
        Assert.Equal(["list"], result.Actions.Added);
        Assert.Empty(result.Actions.Removed);
        Assert.True(_manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task SyncResourcesAsync_ReportsAChangedListObjectsEndpointAsUpdated()
    {
        var existing = Resource("certificates", "Certificates", "/v1/certificates");
        _manager.ResourceRepository.Seed(existing);
        _manager.ActionRepository.Seed(Action("list"));

        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", "/v2/certificates", "list")]);

        Assert.Equal(["certificates"], result.Resources.Updated);
        Assert.Equal("/v2/certificates", existing.ListObjectsEndpoint);
    }

    [Fact]
    public async Task SyncResourcesAsync_ReportsNothingForAnUnchangedResource()
    {
        _manager.ResourceRepository.Seed(Resource("certificates", "Certificates", "/v1/certificates", [Action("list")]));
        _manager.ActionRepository.Seed(Action("list"));

        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", "/v1/certificates", "list")]);

        Assert.Empty(result.Resources.Added);
        Assert.Empty(result.Resources.Updated);
        Assert.Empty(result.Resources.Removed);
        Assert.Empty(result.Actions.Added);
        Assert.Empty(result.Actions.Removed);
    }

    [Fact]
    public async Task SyncResourcesAsync_RemovesAResourceThatIsNoLongerReported()
    {
        _manager.ResourceRepository.Seed(Resource("certificates", "Certificates"), Resource("groups", "Groups"));
        _manager.ActionRepository.Seed(Action("list"));

        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", null, "list")]);

        Assert.Equal(["groups"], result.Resources.Removed);
        Assert.Equal(["certificates"], _manager.ResourceRepository.Stored.Select(r => r.Name));
    }

    [Fact]
    public async Task SyncResourcesAsync_RemovesAnActionThatIsNoLongerReported()
    {
        _manager.ResourceRepository.Seed(Resource("certificates", "Certificates", actions: [Action("list")]));
        _manager.ActionRepository.Seed(Action("list"), Action("revoke"));

        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", null, "list")]);

        Assert.Equal(["revoke"], result.Actions.Removed);
        Assert.Equal(["list"], _manager.ActionRepository.Stored.Select(a => a.Name));
    }

    [Fact]
    public async Task SyncResourcesAsync_RebuildsTheResourceToActionLinksFromScratch()
    {
        var stale = Action("revoke");
        var existing = Resource("certificates", "Certificates", actions: [stale]);
        _manager.ResourceRepository.Seed(existing);
        _manager.ActionRepository.Seed(stale, Action("list"));

        await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", null, "list"), Sync("groups", null, "revoke")]);

        Assert.Equal(["list"], existing.Actions.Select(a => a.Name));
    }

    [Fact]
    public async Task SyncResourcesAsync_SkipsTheWildcardActionName()
    {
        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", null, "ANY")]);

        Assert.Empty(result.Actions.Added);
        Assert.Empty(_manager.ActionRepository.Stored);
    }

    [Fact]
    public async Task SyncResourcesAsync_RollsBackWhenTheSaveFails()
    {
        _manager.ResourceRepository.Seed(Resource("certificates", "Certificates", actions: [Action("list")]));
        _manager.ActionRepository.Seed(Action("list"));
        _manager.OnSave = _ => new InvalidOperationException("deadlock");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServiceFactory.Resource(_manager).SyncResourcesAsync([Sync("certificates", null, "list")]));

        var transaction = _manager.SingleTransaction();
        Assert.True(transaction.RolledBack);
        Assert.False(transaction.Committed);
    }

    [Fact]
    public async Task SyncResourcesAsync_RemovesEverythingWhenNothingIsReported()
    {
        _manager.ResourceRepository.Seed(Resource("certificates", "Certificates"));
        _manager.ActionRepository.Seed(Action("list"));

        var result = await ServiceFactory.Resource(_manager).SyncResourcesAsync([]);

        Assert.Equal(["certificates"], result.Resources.Removed);
        Assert.Equal(["list"], result.Actions.Removed);
        Assert.Empty(_manager.ResourceRepository.Stored);
        Assert.Empty(_manager.ActionRepository.Stored);
    }
}
