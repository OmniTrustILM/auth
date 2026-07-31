using Auth.Controllers;
using Auth.Models.Dto;
using Auth.Services;
using Auth.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Controllers;

public class PermissionsControllerTests
{
    private readonly FakePermissionService _permissions = new();

    private PermissionsController Controller() => new(_permissions);

    private static T Ok<T>(ActionResult<T> result) => (T)Assert.IsType<OkObjectResult>(result.Result).Value!;

    [Fact]
    public async Task GetRolePermissions_RequestsThePermissionsOfThatRole()
    {
        var roleUuid = Guid.NewGuid();

        Assert.Same(_permissions.SubjectPermissions, Ok(await Controller().GetRolePermissions(roleUuid)));
        Assert.Equal([roleUuid], _permissions.RolePermissionsRequested);
    }

    [Fact]
    public async Task GetRoleResourcePermissions_ReturnsTheResourceScopedPermissions()
    {
        var permissions = Ok(await Controller().GetRoleResourcePermissions(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Same(_permissions.ResourcePermissions, permissions);
    }

    [Fact]
    public async Task SaveRolePermissions_ForwardsTheRequestAndReturnsTheStoredResult()
    {
        var roleUuid = Guid.NewGuid();
        var request = new RolePermissionsRequestDto { AllowAllResources = true };

        Assert.Same(_permissions.SubjectPermissions, Ok(await Controller().SaveRolePermissions(roleUuid, request)));

        var saved = Assert.Single(_permissions.SavedRolePermissions);
        Assert.Equal(roleUuid, saved.RoleUuid);
        Assert.Same(request, saved.Permissions);
    }

    [Fact]
    public async Task GetRoleObjectsPermissions_ReturnsTheObjectList()
    {
        var objects = Ok(await Controller().GetRoleObjectsPermissions(Guid.NewGuid(), Guid.NewGuid()));

        Assert.Same(_permissions.ResourcePermissions.Objects, objects);
    }

    [Fact]
    public async Task SaveRoleObjectsPermissions_ForwardsThePayloadAndAnswersWithNoContent()
    {
        var roleUuid = Guid.NewGuid();
        var resourceUuid = Guid.NewGuid();
        List<ObjectPermissionsRequestDto> payload = [new() { Uuid = Guid.NewGuid(), Name = "cert-1" }];

        Assert.IsType<NoContentResult>(await Controller().SaveRoleObjectsPermissions(roleUuid, resourceUuid, payload));

        var saved = Assert.Single(_permissions.SavedObjectsPermissions);
        Assert.Equal(roleUuid, saved.RoleUuid);
        Assert.Equal(resourceUuid, saved.ResourceUuid);
        Assert.Same(payload, saved.Objects);
    }

    [Fact]
    public async Task SaveRoleObjectPermissions_ForwardsEveryIdentifier()
    {
        var roleUuid = Guid.NewGuid();
        var resourceUuid = Guid.NewGuid();
        var objectUuid = Guid.NewGuid();
        var request = new ObjectPermissionsRequestDto { Uuid = objectUuid, Name = "cert-1" };

        Assert.IsType<NoContentResult>(await Controller().SaveRoleObjectPermissions(roleUuid, resourceUuid, objectUuid, request));

        var saved = Assert.Single(_permissions.SavedObjectPermissions);
        Assert.Equal((roleUuid, resourceUuid, objectUuid), (saved.RoleUuid, saved.ResourceUuid, saved.ObjectUuid));
        Assert.Same(request, saved.Permissions);
    }

    [Fact]
    public async Task DeleteRoleObjectPermissions_ForwardsTheIdentifiers()
    {
        var roleUuid = Guid.NewGuid();
        var resourceUuid = Guid.NewGuid();
        var objectUuid = Guid.NewGuid();

        Assert.IsType<NoContentResult>(await Controller().DeleteRoleObjectPermissions(roleUuid, resourceUuid, objectUuid));

        Assert.Equal((roleUuid, resourceUuid, objectUuid), Assert.Single(_permissions.DeletedObjectPermissions));
    }

    [Fact]
    public void DeleteRoleObjectPermissions_DoesNotAnswerUntilTheDeleteHasRun()
    {
        var neverCompletes = new NeverCompletingPermissionService();

        var pending = new PermissionsController(neverCompletes)
            .DeleteRoleObjectPermissions(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid());

        Assert.False(pending.IsCompleted);
    }

    [Fact]
    public async Task GetUserPermissionsAsync_RequestsThePermissionsOfThatUser()
    {
        var userUuid = Guid.NewGuid();

        Assert.Same(_permissions.SubjectPermissions, Ok(await Controller().GetUserPermissionsAsync(userUuid)));
        Assert.Equal([userUuid], _permissions.UserPermissionsRequested);
    }

    private sealed class NeverCompletingPermissionService : IPermissionService
    {
        private readonly TaskCompletionSource _pending = new();

        public bool DeleteCompleted => _pending.Task.IsCompleted;

        public Task DeleteRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid) => _pending.Task;

        public Task<SubjectPermissionsDto> GetRolePermissionsAsync(Guid roleUuid) => throw new NotSupportedException();

        public Task<ResourcePermissionsDto> GetRoleResourcesPermissionsAsync(Guid roleUuid, Guid resourceUuid) => throw new NotSupportedException();

        public Task<SubjectPermissionsDto> SaveRolePermissionsAsync(Guid roleUuid, RolePermissionsRequestDto rolePermissions) => throw new NotSupportedException();

        public Task<List<ObjectPermissionsDto>> GetRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid) => throw new NotSupportedException();

        public Task SaveRoleObjectsPermissionsAsync(Guid roleUuid, Guid resourceUuid, List<ObjectPermissionsRequestDto> objectsPermissions) => throw new NotSupportedException();

        public Task SaveRoleObjectPermissionsAsync(Guid roleUuid, Guid resourceUuid, Guid objectUuid, ObjectPermissionsRequestDto objectPermissions) => throw new NotSupportedException();

        public Task<SubjectPermissionsDto> GetUserPermissionsAsync(Guid userUuid) => throw new NotSupportedException();
    }
}
