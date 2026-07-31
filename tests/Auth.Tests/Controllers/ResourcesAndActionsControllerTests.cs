using Auth.Common.Models.Dto;
using Auth.Controllers;
using Auth.Models.Dto;
using Auth.Tests.TestSupport;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Tests.Controllers;

public class ResourcesAndActionsControllerTests
{
    private readonly FakeResourceService _resources = new();
    private readonly FakeActionService _actions = new();

    private static T Ok<T>(ActionResult<T> result) => (T)Assert.IsType<OkObjectResult>(result.Result).Value!;

    [Fact]
    public async Task GetResourcesAsync_ReturnsEveryResource()
    {
        var resources = Ok(await new ResourcesController(_resources).GetResourcesAsync());

        Assert.Same(_resources.AllResources, resources);
        Assert.Equal(1, _resources.AllResourcesRequested);
    }

    [Fact]
    public async Task AddResourcesAsync_ForwardsThePayloadAndAnswersWithNoContent()
    {
        List<ResourceSyncRequestDto> payload = [new() { Name = "certificates", Actions = ["list"] }];

        Assert.IsType<NoContentResult>(await new ResourcesController(_resources).AddResourcesAsync(payload));
        Assert.Same(payload, Assert.Single(_resources.Added));
    }

    [Fact]
    public async Task SyncResourcesAsync_ReturnsTheSyncReport()
    {
        List<ResourceSyncRequestDto> payload = [new() { Name = "certificates", Actions = ["list"] }];

        var report = Ok(await new ResourcesController(_resources).SyncResourcesAsync(payload));

        Assert.Same(_resources.SyncResult, report);
        Assert.Same(payload, Assert.Single(_resources.Synced));
    }

    [Fact]
    public async Task GetActionsAsync_AsksForEveryAction()
    {
        var page = Ok(await new ActionsController(_actions).GetActionsAsync());

        Assert.Same(_actions.Page, page);
        Assert.IsType<QueryRequestDto>(Assert.Single(_actions.Queries));
    }
}
