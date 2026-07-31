using Auth.Common.Exceptions;
using Auth.Models.Entities;
using Auth.Services;
using Auth.Tests.TestSupport;
using Microsoft.Extensions.Logging;

namespace Auth.Tests.Services;

/// <summary>
/// The role-facing queries, which add the verbose expansion on top of the merge: an object's allow list is filled in
/// with the resource actions the role holds and the object does not deny.
/// </summary>
public class PermissionServiceQueryTests
{
    private readonly PermissionWorld _world = new();

    [Fact]
    public async Task RolePermissions_ReportTheRoleWideGrant()
    {
        _world.AllowAllResources();

        Assert.True((await ServiceFactory.Permission(_world.Manager).GetRolePermissionsAsync(_world.Role.Uuid)).AllowAllResources);
    }

    [Fact]
    public async Task AnObjectInheritsEveryResourceAction_WhenTheRoleAllowsThemAll()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "detail", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.AllResourceActions(certificates);
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        var permissions = await ServiceFactory.Permission(_world.Manager).GetRolePermissionsAsync(_world.Role.Uuid);
        var objectPermissions = Assert.Single(Assert.Single(permissions.Resources).Objects);

        Assert.Equal(["detail", "list"], objectPermissions.Allow);
        Assert.Equal(["revoke"], objectPermissions.Deny);
    }

    [Fact]
    public async Task AnObjectInheritsOnlyTheActionsTheRoleHolds()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "detail", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ResourceAction(certificates, "list");
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);

        var permissions = await ServiceFactory.Permission(_world.Manager).GetRolePermissionsAsync(_world.Role.Uuid);
        var objectPermissions = Assert.Single(Assert.Single(permissions.Resources).Objects);

        Assert.Equal(["list", "revoke"], objectPermissions.Allow);
    }

    [Fact]
    public async Task AnInheritedActionIsNotDuplicated_WhenTheObjectAlreadyAllowsIt()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list");
        var objectUuid = Guid.NewGuid();
        _world.AllResourceActions(certificates);
        _world.ObjectAction(certificates, "list", objectUuid, "cert-1", isAllowed: true);

        var permissions = await ServiceFactory.Permission(_world.Manager).GetRolePermissionsAsync(_world.Role.Uuid);

        Assert.Equal(["list"], Assert.Single(Assert.Single(permissions.Resources).Objects).Allow);
    }

    [Fact]
    public async Task AResourceThatNoLongerExists_IsReportedAsAWarningRatherThanFailing()
    {
        var logger = new RecordingLogger<PermissionService>();
        var vanished = new Resource { Name = "vanished", DisplayName = "Vanished", Actions = [], Permissions = [] };
        _world.ResourceAction(vanished, "list");

        var permissions = await ServiceFactory.Permission(_world.Manager, logger).GetRolePermissionsAsync(_world.Role.Uuid);

        Assert.Equal("vanished", Assert.Single(permissions.Resources).Name);
        Assert.True(logger.Logged(LogLevel.Warning, "Missing resource vanished"));
    }

    [Fact]
    public async Task RolePermissions_ReportAnUnknownRoleAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(_world.Manager).GetRolePermissionsAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task ResourcePermissions_ReportTheRolesGrantOnThatResourceAlone()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var groups = _world.AddResource("groups", actionNames: ["list"]);
        _world.ResourceAction(certificates, "list");
        _world.ResourceAction(groups, "list");

        var permissions = await ServiceFactory.Permission(_world.Manager)
            .GetRoleResourcesPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        Assert.Equal("certificates", permissions.Name);
        Assert.Equal(["list"], permissions.Actions);
    }

    [Fact]
    public async Task ResourcePermissions_ExpandTheObjectsOfThatResource()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.AllResourceActions(certificates);
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        var permissions = await ServiceFactory.Permission(_world.Manager)
            .GetRoleResourcesPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        var objectPermissions = Assert.Single(permissions.Objects);
        Assert.Equal(["list"], objectPermissions.Allow);
        Assert.Equal(["revoke"], objectPermissions.Deny);
    }

    [Fact]
    public async Task ResourcePermissions_FallBackToAnEmptyGrantWhenTheRoleHasNoRowForTheResource()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list");

        var permissions = await ServiceFactory.Permission(_world.Manager)
            .GetRoleResourcesPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        Assert.Equal("certificates", permissions.Name);
        Assert.False(permissions.AllowAllActions);
        Assert.Empty(permissions.Actions);
        Assert.Empty(permissions.Objects);
    }

    [Fact]
    public async Task ResourcePermissions_StillFallBackWhenTheRoleHoldsAResourceWideGrant()
    {
        // The repository filters by resource UUID, so the row that carries no resource never reaches the merge here.
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list");
        _world.AllowAllResources();

        var permissions = await ServiceFactory.Permission(_world.Manager)
            .GetRoleResourcesPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        Assert.False(permissions.AllowAllActions);
    }

    [Fact]
    public async Task ResourcePermissions_ReportAnUnknownResourceAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(_world.Manager).GetRoleResourcesPermissionsAsync(_world.Role.Uuid, Guid.NewGuid()));
    }

    [Fact]
    public async Task ResourcePermissions_ReportAnUnknownRoleAsNotFound()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list");

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(_world.Manager).GetRoleResourcesPermissionsAsync(Guid.NewGuid(), certificates.Uuid));
    }

    [Fact]
    public async Task ObjectsPermissions_ReturnJustTheObjectListOfTheResource()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: true);

        var objects = await ServiceFactory.Permission(_world.Manager)
            .GetRoleObjectsPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        Assert.Equal(objectUuid, Assert.Single(objects).Uuid);
    }

    [Fact]
    public async Task ObjectsPermissions_AreEmptyWhenNoObjectIsScoped()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list");
        _world.ResourceAction(certificates, "list");

        var objects = await ServiceFactory.Permission(_world.Manager)
            .GetRoleObjectsPermissionsAsync(_world.Role.Uuid, certificates.Uuid);

        Assert.Empty(objects);
    }
}
