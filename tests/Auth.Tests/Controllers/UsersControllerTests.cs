using Auth.Controllers;
using Auth.Models.Dto;
using Auth.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Controllers;

public class UsersControllerTests
{
    private readonly FakeUserService _users = new();
    private readonly FakeRoleService _roles = new();

    private UsersController Controller() => new(_users);

    private static T Ok<T>(ActionResult<T> result) => (T)Assert.IsType<OkObjectResult>(result.Result).Value!;

    [Fact]
    public async Task AuthenticateUserAsync_ForwardsTheRequestAndReturnsTheResponse()
    {
        var request = new AuthenticationRequestDto { SystemUsername = "core" };

        var response = Ok(await Controller().AuthenticateUserAsync(request));

        Assert.Same(_users.AuthenticationResponse, response);
        Assert.Same(request, Assert.Single(_users.Authenticated));
    }

    [Fact]
    public async Task IdentifyUserAsync_ForwardsTheRequestAndReturnsTheUser()
    {
        var request = new AuthenticationRequestDto { CertificateContent = "content" };

        var user = Ok(await Controller().IdentifyUserAsync(request));

        Assert.Same(_users.Detail, user);
        Assert.Same(request, Assert.Single(_users.Identified));
    }

    [Fact]
    public async Task GetUsersAsync_PassesTheGroupFilterThrough()
    {
        var page = Ok(await Controller().GetUsersAsync("operators"));

        Assert.Same(_users.Page, page);
        var query = Assert.IsType<UserQueryRequestDto>(Assert.Single(_users.Queries));
        Assert.Equal("operators", query.Group);
    }

    [Fact]
    public async Task GetUsersAsync_AsksForEveryUserWhenNoGroupIsGiven()
    {
        await Controller().GetUsersAsync(null);

        Assert.Null(Assert.IsType<UserQueryRequestDto>(Assert.Single(_users.Queries)).Group);
    }

    [Fact]
    public async Task CreateUserAsync_ReportsTheNewLocation()
    {
        var request = new UserRequestDto { Username = "jane" };

        var result = await Controller().CreateUserAsync(request);

        var created = Assert.IsType<CreatedResult>(result.Result);
        Assert.Equal("auth/users", created.Location);
        Assert.Same(_users.Detail, created.Value);
        Assert.Same(request, Assert.Single(_users.Created));
    }

    [Fact]
    public async Task GetUserAsync_RequestsTheUserByUuid()
    {
        var uuid = Guid.NewGuid();

        var user = Ok(await Controller().GetUserAsync(uuid));

        Assert.Same(_users.Detail, user);
        Assert.Equal([uuid], _users.DetailsRequested);
    }

    [Fact]
    public async Task UpdateUserAsync_ForwardsTheUuidAndTheRequest()
    {
        var uuid = Guid.NewGuid();
        var request = new UserUpdateRequestDto { FirstName = "Jane" };

        var user = Ok(await Controller().UpdateUserAsync(uuid, request));

        Assert.Same(_users.Detail, user);
        var update = Assert.Single(_users.Updated);
        Assert.Equal(uuid, update.Key);
        Assert.Same(request, update.Dto);
    }

    [Fact]
    public async Task DeleteUserAsync_AnswersWithNoContent()
    {
        var uuid = Guid.NewGuid();

        Assert.IsType<NoContentResult>(await Controller().DeleteUserAsync(uuid));
        Assert.Equal([uuid], _users.Deleted);
    }

    [Fact]
    public async Task EnableUserAsync_AsksForTheUserToBeEnabled()
    {
        var uuid = Guid.NewGuid();

        Ok(await Controller().EnableUserAsync(uuid));

        Assert.Equal((uuid, true), Assert.Single(_users.EnableCalls));
    }

    [Fact]
    public async Task DisableUserAsync_AsksForTheUserToBeDisabled()
    {
        var uuid = Guid.NewGuid();

        Ok(await Controller().DisableUserAsync(uuid));

        Assert.Equal((uuid, false), Assert.Single(_users.EnableCalls));
    }

    [Fact]
    public async Task GetUserRolesAsync_ReadsThroughTheInjectedRoleService()
    {
        var uuid = Guid.NewGuid();

        var roles = Ok(await Controller().GetUserRolesAsync(_roles, uuid));

        Assert.Same(_roles.UserRoles, roles);
        Assert.Equal([uuid], _roles.UserRolesRequested);
    }

    [Fact]
    public async Task AssignRolesAsync_ForwardsTheWholeRoleSet()
    {
        var uuid = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        Ok(await Controller().AssignRolesAsync(uuid, [first, second]));

        var assignment = Assert.Single(_users.RoleSetsAssigned);
        Assert.Equal(uuid, assignment.UserUuid);
        Assert.Equal([first, second], assignment.RoleUuids);
    }

    [Fact]
    public async Task AssignRoleAsync_ForwardsBothUuids()
    {
        var userUuid = Guid.NewGuid();
        var roleUuid = Guid.NewGuid();

        Ok(await Controller().AssignRoleAsync(userUuid, roleUuid));

        Assert.Equal((userUuid, roleUuid), Assert.Single(_users.RolesAssigned));
    }

    [Fact]
    public async Task RemoveRoleAsync_ForwardsBothUuids()
    {
        var userUuid = Guid.NewGuid();
        var roleUuid = Guid.NewGuid();

        Ok(await Controller().RemoveRoleAsync(userUuid, roleUuid));

        Assert.Equal((userUuid, roleUuid), Assert.Single(_users.RolesRemoved));
    }
}
