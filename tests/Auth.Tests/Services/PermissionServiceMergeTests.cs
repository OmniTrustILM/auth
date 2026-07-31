using Auth.Common.Exceptions;
using Auth.Tests.TestSupport;

namespace Auth.Tests.Services;

/// <summary>
/// The raw merge, read through the user-facing query because that one returns the merge result without the verbose
/// expansion on top.
/// </summary>
public class PermissionServiceMergeTests
{
    private readonly PermissionWorld _world = new();

    private Task<Auth.Models.Dto.SubjectPermissionsDto> UserPermissions()
        => ServiceFactory.Permission(_world.Manager).GetUserPermissionsAsync(_world.User.Uuid);

    [Fact]
    public async Task NoRows_MergeToNoPermissions()
    {
        var permissions = await UserPermissions();

        Assert.False(permissions.AllowAllResources);
        Assert.Empty(permissions.Resources);
    }

    [Fact]
    public async Task ARowWithoutAResource_GrantsEveryResource()
    {
        _world.AllowAllResources();

        Assert.True((await UserPermissions()).AllowAllResources);
    }

    [Fact]
    public async Task ARowWithoutAResourceThatDenies_GrantsNothingOnItsOwn()
    {
        _world.AllowAllResources(isAllowed: false);

        Assert.False((await UserPermissions()).AllowAllResources);
    }

    [Fact]
    public async Task AllowBeatsDenyAcrossRows_LeavingTheOverrideToTheLowerLevel()
    {
        _world.AllowAllResources(isAllowed: false);
        _world.AllowAllResources();

        Assert.True((await UserPermissions()).AllowAllResources);
    }

    [Fact]
    public async Task ARowWithoutAnAction_GrantsEveryActionOnThatResource()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list", "detail"]);
        _world.AllResourceActions(certificates);

        var resource = Assert.Single((await UserPermissions()).Resources);

        Assert.Equal("certificates", resource.Name);
        Assert.True(resource.AllowAllActions);
        Assert.Empty(resource.Actions);
    }

    [Fact]
    public async Task AllowAllActionsBeatsDenyAcrossRows()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list"]);
        _world.AllResourceActions(certificates, isAllowed: false);
        _world.AllResourceActions(certificates);

        Assert.True(Assert.Single((await UserPermissions()).Resources).AllowAllActions);
    }

    [Fact]
    public async Task AllowedResourceActions_AreListedSorted()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list", "detail", "issue"]);
        _world.ResourceAction(certificates, "list");
        _world.ResourceAction(certificates, "detail");
        _world.ResourceAction(certificates, "issue");

        Assert.Equal(["detail", "issue", "list"], Assert.Single((await UserPermissions()).Resources).Actions);
    }

    [Fact]
    public async Task ADeniedResourceActionIsDropped_BecauseDenyingForEveryObjectIsNotSupported()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list", "revoke"]);
        _world.ResourceAction(certificates, "list");
        _world.ResourceAction(certificates, "revoke", isAllowed: false);

        Assert.Equal(["list"], Assert.Single((await UserPermissions()).Resources).Actions);
    }

    [Fact]
    public async Task IndividualActionsAreNotListed_WhenTheResourceAlreadyAllowsThemAll()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list"]);
        _world.AllResourceActions(certificates);
        _world.ResourceAction(certificates, "list");

        var resource = Assert.Single((await UserPermissions()).Resources);

        Assert.True(resource.AllowAllActions);
        Assert.Empty(resource.Actions);
    }

    [Fact]
    public async Task AnObjectRow_GrantsTheActionOnThatObjectAlone()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);

        var resource = Assert.Single((await UserPermissions()).Resources);
        var permissions = Assert.Single(resource.Objects);

        Assert.Equal(objectUuid, permissions.Uuid);
        Assert.Equal("cert-1", permissions.Name);
        Assert.Equal(["revoke"], permissions.Allow);
        Assert.Empty(permissions.Deny);
        Assert.Empty(resource.Actions);
    }

    [Fact]
    public async Task AnObjectAllowIsDropped_WhenTheResourceAlreadyAllowsThatAction()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ResourceAction(certificates, "list");
        _world.ObjectAction(certificates, "list", objectUuid, "cert-1", isAllowed: true);
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);

        Assert.Equal(["revoke"], Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects).Allow);
    }

    [Fact]
    public async Task AnObjectDeny_IsReportedOnTheObject()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        var permissions = Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects);

        Assert.Equal(["revoke"], permissions.Deny);
        Assert.Empty(permissions.Allow);
    }

    [Fact]
    public async Task ADeniedActionIsNotAlsoAllowed_WhenBothRowsExistForTheSameObject()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        var permissions = Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects);

        Assert.Equal(["revoke"], permissions.Deny);
        Assert.Empty(permissions.Allow);
    }

    [Fact]
    public async Task ObjectDenyListsAreSorted()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke", "delete", "archive");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);
        _world.ObjectAction(certificates, "delete", objectUuid, "cert-1", isAllowed: false);
        _world.ObjectAction(certificates, "archive", objectUuid, "cert-1", isAllowed: false);

        Assert.Equal(["archive", "delete", "revoke"], Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects).Deny);
    }

    [Fact]
    public async Task SeveralRowsForOneObject_CollapseIntoASingleEntry()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);
        _world.ObjectAction(certificates, "list", objectUuid, "cert-1", isAllowed: true);

        var permissions = Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects);

        Assert.Equal(objectUuid, permissions.Uuid);
        Assert.Equal("cert-1", permissions.Name);
        Assert.Equal(["list", "revoke"], permissions.Allow.Order());
    }

    [Fact]
    public async Task ObjectAllowsAreLeftToTheVerboseExpansion_WhenTheResourceAllowsEveryAction()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.AllResourceActions(certificates);
        _world.ObjectAction(certificates, "list", objectUuid, "cert-1", isAllowed: true);
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        var permissions = Assert.Single(Assert.Single((await UserPermissions()).Resources).Objects);

        Assert.Empty(permissions.Allow);
        Assert.Equal(["revoke"], permissions.Deny);
    }

    [Fact]
    public async Task SeveralObjectsOfOneResource_AreReportedSeparately()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", first, "cert-1", isAllowed: true);
        _world.ObjectAction(certificates, "revoke", second, "cert-2", isAllowed: false);

        var objects = Assert.Single((await UserPermissions()).Resources).Objects;

        Assert.Equal(2, objects.Count);
        Assert.Equal(["revoke"], objects.Single(o => o.Uuid == first).Allow);
        Assert.Equal(["revoke"], objects.Single(o => o.Uuid == second).Deny);
    }

    [Fact]
    public async Task ResourcesAreReportedInNameOrder()
    {
        var groups = _world.AddResource("groups", actionNames: ["list"]);
        var certificates = _world.AddResource("certificates", actionNames: ["list"]);
        var authorities = _world.AddResource("authorities", actionNames: ["list"]);
        _world.ResourceAction(groups, "list");
        _world.ResourceAction(certificates, "list");
        _world.ResourceAction(authorities, "list");

        Assert.Equal(["authorities", "certificates", "groups"], (await UserPermissions()).Resources.Select(r => r.Name));
    }

    [Fact]
    public async Task PermissionsOfARoleTheUserDoesNotHold_AreNotVisible()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list"]);
        _world.ResourceAction(certificates, "list");

        var outsider = new Auth.Models.Entities.User { Username = "john", Roles = [] };
        _world.Manager.UserRepository.Seed(outsider);

        var permissions = await ServiceFactory.Permission(_world.Manager).GetUserPermissionsAsync(outsider.Uuid);

        Assert.False(permissions.AllowAllResources);
        Assert.Empty(permissions.Resources);
    }

    [Fact]
    public async Task AnUnknownUser_IsReportedAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(_world.Manager).GetUserPermissionsAsync(Guid.NewGuid()));
    }
}
