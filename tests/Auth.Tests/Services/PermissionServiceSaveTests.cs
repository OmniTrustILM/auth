using Auth.Common.Exceptions;
using Auth.Models.Dto;
using Auth.Tests.TestSupport;

namespace Auth.Tests.Services;

public class PermissionServiceSaveTests
{
    private readonly PermissionWorld _world = new();

    private FakeRepositoryManager Manager => _world.Manager;

    private static ResourcePermissionsRequestDto ResourceRequest(
        string name,
        bool allowAllActions = false,
        List<string>? actions = null,
        List<ObjectPermissionsRequestDto>? objects = null)
        => new() { Name = name, AllowAllActions = allowAllActions, Actions = actions, Objects = objects };

    private static ObjectPermissionsRequestDto ObjectRequest(Guid uuid, string name, List<string>? allow = null, List<string>? deny = null)
        => new() { Uuid = uuid, Name = name, Allow = allow, Deny = deny };

    [Fact]
    public async Task ARoleWideGrant_IsStoredAsARowWithoutAResource()
    {
        var permissions = await ServiceFactory.Permission(Manager)
            .SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto { AllowAllResources = true });

        var stored = Assert.Single(Manager.PermissionRepository.Stored);
        Assert.Null(stored.ResourceUuid);
        Assert.Null(stored.ActionUuid);
        Assert.True(stored.IsAllowed);
        Assert.True(permissions.AllowAllResources);
        Assert.True(Manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task SavingReplacesTheRolesPreviousNonObjectRows()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list", "revoke"]);
        _world.ResourceAction(certificates, "revoke");

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", actions: ["list"])],
        });

        var stored = Assert.Single(Manager.PermissionRepository.Stored);
        Assert.Equal("list", stored.Action!.Name);
    }

    [Fact]
    public async Task SavingLeavesObjectScopedRowsAlone_WhenTheRequestCarriesNoObjectsForTheResource()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", objectUuid, "cert-1", isAllowed: false);

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", actions: ["list"])],
        });

        Assert.Contains(Manager.PermissionRepository.Stored, p => p.ObjectUuid == objectUuid && !p.IsAllowed);
    }

    [Fact]
    public async Task AResourceWideGrant_IsStoredAsARowWithoutAnAction()
    {
        _world.AddResource("certificates", actionNames: ["list"]);

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", allowAllActions: true)],
        });

        var stored = Assert.Single(Manager.PermissionRepository.Stored);
        Assert.NotNull(stored.ResourceUuid);
        Assert.Null(stored.ActionUuid);
        Assert.True(stored.IsAllowed);
    }

    [Fact]
    public async Task EachRequestedAction_BecomesItsOwnRow()
    {
        _world.AddResource("certificates", actionNames: ["list", "detail"]);

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", actions: ["list", "detail"])],
        });

        Assert.Equal(["detail", "list"], Manager.PermissionRepository.Stored.Select(p => p.Action!.Name).Order());
    }

    [Fact]
    public async Task IndividualActionsAreSkipped_WhenTheResourceGrantIsWideOpen()
    {
        _world.AddResource("certificates", actionNames: ["list"]);

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", allowAllActions: true, actions: ["list"])],
        });

        Assert.Null(Assert.Single(Manager.PermissionRepository.Stored).ActionUuid);
    }

    [Fact]
    public async Task AnUnknownResourceName_IsRejectedAndRolledBack()
    {
        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("nosuch", actions: ["list"])],
            }));

        Assert.Contains("nosuch", exception.Message);
        Assert.True(Manager.SingleTransaction().RolledBack);
        Assert.Empty(Manager.PermissionRepository.Stored);
    }

    [Fact]
    public async Task AnUnknownActionName_IsRejectedAndRolledBack()
    {
        _world.AddResource("certificates", actionNames: ["list"]);

        var exception = await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("certificates", actions: ["nosuch"])],
            }));

        Assert.Contains("nosuch", exception.Message);
        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task ObjectPermissions_AreRefusedOnAResourceThatCannotListItsObjects()
    {
        _world.AddResource("certificates", listObjectsEndpoint: null, actionNames: ["revoke"]);

        var exception = await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("certificates", actions: ["revoke"], objects: [ObjectRequest(Guid.NewGuid(), "cert-1", deny: ["revoke"])])],
            }));

        Assert.Contains("does not support object access permissions", exception.Message);
        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task AnEmptyObjectList_IsAcceptedEvenWhenTheResourceCannotListItsObjects()
    {
        _world.AddResource("certificates", listObjectsEndpoint: null, actionNames: ["list"]);

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", actions: ["list"], objects: [])],
        });

        Assert.True(Manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task AnObjectAllow_IsStoredOnlyForActionsTheResourceGrantDoesNotAlreadyCover()
    {
        _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", actions: ["list"], objects: [ObjectRequest(objectUuid, "cert-1", allow: ["list", "revoke"])])],
        });

        var objectRow = Assert.Single(Manager.PermissionRepository.Stored, p => p.ObjectUuid == objectUuid);
        Assert.Equal("revoke", objectRow.Action!.Name);
        Assert.Equal("cert-1", objectRow.ObjectName);
        Assert.True(objectRow.IsAllowed);
    }

    [Fact]
    public async Task NoObjectAllowIsStored_WhenTheResourceGrantIsWideOpen()
    {
        _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var objectUuid = Guid.NewGuid();

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources =
            [
                ResourceRequest("certificates", allowAllActions: true,
                    objects: [ObjectRequest(objectUuid, "cert-1", allow: ["list"], deny: ["revoke"])]),
            ],
        });

        var objectRows = Manager.PermissionRepository.Stored.Where(p => p.ObjectUuid == objectUuid).ToList();
        var denied = Assert.Single(objectRows);
        Assert.Equal("revoke", denied.Action!.Name);
        Assert.False(denied.IsAllowed);
    }

    [Fact]
    public async Task AnObjectDeny_IsStoredAsADisallowedRow()
    {
        _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();

        await ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
        {
            Resources = [ResourceRequest("certificates", objects: [ObjectRequest(objectUuid, "cert-1", deny: ["revoke"])])],
        });

        var stored = Assert.Single(Manager.PermissionRepository.Stored);
        Assert.Equal(objectUuid, stored.ObjectUuid);
        Assert.False(stored.IsAllowed);
    }

    [Fact]
    public async Task AnUnknownActionInAnObjectAllow_IsRejected()
    {
        _world.AddResource("certificates", "/v1/certificates", "list");

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("certificates", actions: [], objects: [ObjectRequest(Guid.NewGuid(), "cert-1", allow: ["nosuch"])])],
            }));

        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task AnUnknownActionInAnObjectDeny_IsRejected()
    {
        _world.AddResource("certificates", "/v1/certificates", "list");

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("certificates", objects: [ObjectRequest(Guid.NewGuid(), "cert-1", deny: ["nosuch"])])],
            }));

        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task ARequestWithNoResources_ClearsTheRolesNonObjectRows()
    {
        var certificates = _world.AddResource("certificates", actionNames: ["list"]);
        _world.ResourceAction(certificates, "list");

        var permissions = await ServiceFactory.Permission(Manager)
            .SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto { Resources = null });

        Assert.Empty(Manager.PermissionRepository.Stored);
        Assert.Empty(permissions.Resources);
    }

    [Fact]
    public async Task AFailingSave_RollsBackAndPropagates()
    {
        _world.AddResource("certificates", actionNames: ["list"]);
        Manager.OnSave = _ => new InvalidOperationException("deadlock");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(_world.Role.Uuid, new RolePermissionsRequestDto
            {
                Resources = [ResourceRequest("certificates", actions: ["list"])],
            }));

        var transaction = Manager.SingleTransaction();
        Assert.True(transaction.RolledBack);
        Assert.False(transaction.Committed);
    }

    [Fact]
    public async Task AnUnknownRole_IsRejectedBeforeATransactionIsOpened()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRolePermissionsAsync(Guid.NewGuid(), new RolePermissionsRequestDto()));

        Assert.Empty(Manager.Transactions);
    }

    [Fact]
    public async Task SavingObjectsPermissions_ReplacesTheObjectRowsOfThatResource()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "list", "revoke");
        var stale = Guid.NewGuid();
        var fresh = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", stale, "cert-stale", isAllowed: false);

        await ServiceFactory.Permission(Manager)
            .SaveRoleObjectsPermissionsAsync(_world.Role.Uuid, certificates.Uuid, [ObjectRequest(fresh, "cert-fresh", deny: ["revoke"])]);

        var stored = Assert.Single(Manager.PermissionRepository.Stored);
        Assert.Equal(fresh, stored.ObjectUuid);
        Assert.True(Manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task SavingObjectsPermissions_IsRefusedOnAResourceThatCannotListItsObjects()
    {
        var certificates = _world.AddResource("certificates", listObjectsEndpoint: null, actionNames: ["revoke"]);

        await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Permission(Manager)
                .SaveRoleObjectsPermissionsAsync(_world.Role.Uuid, certificates.Uuid, [ObjectRequest(Guid.NewGuid(), "cert-1", deny: ["revoke"])]));

        Assert.Empty(Manager.Transactions);
    }

    [Fact]
    public async Task SavingObjectsPermissions_ReportsAnUnknownResourceAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).SaveRoleObjectsPermissionsAsync(_world.Role.Uuid, Guid.NewGuid(), []));
    }

    [Fact]
    public async Task SavingObjectsPermissions_RollsBackAFailingSave()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        Manager.OnSave = _ => new InvalidOperationException("deadlock");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServiceFactory.Permission(Manager)
                .SaveRoleObjectsPermissionsAsync(_world.Role.Uuid, certificates.Uuid, [ObjectRequest(Guid.NewGuid(), "cert-1", deny: ["revoke"])]));

        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task SavingOneObjectsPermissions_ReplacesThatObjectAlone()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var kept = Guid.NewGuid();
        var replaced = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", kept, "cert-kept", isAllowed: false);
        _world.ObjectAction(certificates, "revoke", replaced, "cert-old", isAllowed: true);

        await ServiceFactory.Permission(Manager)
            .SaveRoleObjectPermissionsAsync(_world.Role.Uuid, certificates.Uuid, replaced, ObjectRequest(replaced, "cert-new", deny: ["revoke"]));

        Assert.Contains(Manager.PermissionRepository.Stored, p => p.ObjectUuid == kept);
        var rewritten = Assert.Single(Manager.PermissionRepository.Stored, p => p.ObjectUuid == replaced);
        Assert.Equal("cert-new", rewritten.ObjectName);
        Assert.False(rewritten.IsAllowed);
        Assert.True(Manager.SingleTransaction().Committed);
    }

    [Fact]
    public async Task SavingOneObjectsPermissions_IsRefusedOnAResourceThatCannotListItsObjects()
    {
        var certificates = _world.AddResource("certificates", listObjectsEndpoint: null, actionNames: ["revoke"]);
        var objectUuid = Guid.NewGuid();

        await Assert.ThrowsAsync<InvalidActionException>(
            () => ServiceFactory.Permission(Manager)
                .SaveRoleObjectPermissionsAsync(_world.Role.Uuid, certificates.Uuid, objectUuid, ObjectRequest(objectUuid, "cert-1", deny: ["revoke"])));

        Assert.Empty(Manager.Transactions);
    }

    [Fact]
    public async Task SavingOneObjectsPermissions_RollsBackAFailingSave()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var objectUuid = Guid.NewGuid();
        Manager.OnSave = _ => new InvalidOperationException("deadlock");

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => ServiceFactory.Permission(Manager)
                .SaveRoleObjectPermissionsAsync(_world.Role.Uuid, certificates.Uuid, objectUuid, ObjectRequest(objectUuid, "cert-1", deny: ["revoke"])));

        Assert.True(Manager.SingleTransaction().RolledBack);
    }

    [Fact]
    public async Task DeletingObjectPermissions_RemovesThatObjectsRowsWithoutATransaction()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");
        var removed = Guid.NewGuid();
        var kept = Guid.NewGuid();
        _world.ObjectAction(certificates, "revoke", removed, "cert-removed", isAllowed: false);
        _world.ObjectAction(certificates, "revoke", kept, "cert-kept", isAllowed: false);

        await ServiceFactory.Permission(Manager).DeleteRoleObjectPermissionsAsync(_world.Role.Uuid, certificates.Uuid, removed);

        Assert.Equal(kept, Assert.Single(Manager.PermissionRepository.Stored).ObjectUuid);
        Assert.Equal(1, Manager.SaveCount);
        Assert.Empty(Manager.Transactions);
    }

    [Fact]
    public async Task DeletingObjectPermissions_ReportsAnUnknownRoleAsNotFound()
    {
        var certificates = _world.AddResource("certificates", "/v1/certificates", "revoke");

        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).DeleteRoleObjectPermissionsAsync(Guid.NewGuid(), certificates.Uuid, Guid.NewGuid()));
    }

    [Fact]
    public async Task DeletingObjectPermissions_ReportsAnUnknownResourceAsNotFound()
    {
        await Assert.ThrowsAsync<EntityNotFoundException>(
            () => ServiceFactory.Permission(Manager).DeleteRoleObjectPermissionsAsync(_world.Role.Uuid, Guid.NewGuid(), Guid.NewGuid()));
    }
}
