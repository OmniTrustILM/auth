using Auth.Common.Models.Dto;
using Auth.Controllers;
using Auth.Models.Dto;
using Auth.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Controllers;

public class RolesControllerTests
{
    private readonly FakeRoleService _roles = new();
    private readonly FakeUserService _users = new();

    private RolesController Controller() => new(_roles);

    private static T Ok<T>(ActionResult<T> result) => (T)Assert.IsType<OkObjectResult>(result.Result).Value!;

    [Fact]
    public async Task GetRolesAsync_AsksForEveryRole()
    {
        var page = Ok(await Controller().GetRolesAsync());

        Assert.Same(_roles.Page, page);
        Assert.IsType<QueryRequestDto>(Assert.Single(_roles.Queries));
    }

    [Fact]
    public async Task CreateRoleAsync_ReportsTheNewLocation()
    {
        var request = new RoleRequestDto { Name = "admin" };

        var created = Assert.IsType<CreatedResult>((await Controller().CreateRoleAsync(request)).Result);

        Assert.Equal("auth/roles", created.Location);
        Assert.Same(_roles.Detail, created.Value);
        Assert.Same(request, Assert.Single(_roles.Created));
    }

    [Fact]
    public async Task GetRoleAsync_RequestsTheRoleByUuid()
    {
        var uuid = Guid.NewGuid();

        Assert.Same(_roles.Detail, Ok(await Controller().GetRoleAsync(uuid)));
        Assert.Equal([uuid], _roles.DetailsRequested);
    }

    [Fact]
    public async Task UpdateRoleAsync_ForwardsTheUuidAndTheRequest()
    {
        var uuid = Guid.NewGuid();
        var request = new RoleUpdateRequestDto { Description = "changed" };

        Assert.Same(_roles.Detail, Ok(await Controller().UpdateRoleAsync(uuid, request)));

        var update = Assert.Single(_roles.Updated);
        Assert.Equal(uuid, update.Key);
        Assert.Same(request, update.Dto);
    }

    [Fact]
    public async Task DeleteRoleAsync_AnswersWithNoContent()
    {
        var uuid = Guid.NewGuid();

        Assert.IsType<NoContentResult>(await Controller().DeleteRoleAsync(uuid));
        Assert.Equal([uuid], _roles.Deleted);
    }

    [Fact]
    public async Task GetRoleUsersAsync_ReadsThroughTheInjectedUserService()
    {
        var uuid = Guid.NewGuid();

        Assert.Same(_users.RoleUsers, Ok(await Controller().GetRoleUsersAsync(_users, uuid)));
        Assert.Equal([uuid], _users.RoleUsersRequested);
    }

    [Fact]
    public async Task AssignRoleUsersAsync_ForwardsTheWholeMembership()
    {
        var roleUuid = Guid.NewGuid();
        var first = Guid.NewGuid();
        var second = Guid.NewGuid();

        Assert.Same(_roles.Detail, Ok(await Controller().AssignRoleUsersAsync(roleUuid, [first, second])));

        var assignment = Assert.Single(_roles.UsersAssigned);
        Assert.Equal(roleUuid, assignment.RoleUuid);
        Assert.Equal([first, second], assignment.UserUuids);
    }
}
