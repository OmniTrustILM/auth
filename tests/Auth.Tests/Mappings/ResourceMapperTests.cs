using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Models.Mappings;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Mappings;

public class ResourceMapperTests
{
    private static Resource StoredResource() => new()
    {
        Uuid = Guid.NewGuid(),
        Name = "certificates",
        DisplayName = "Certificates",
        ListObjectsEndpoint = "/v1/certificates",
        Actions = [],
    };

    [Fact]
    public void CreateRequest_BecomesAResource()
    {
        var resource = new ResourceRequestDto
        {
            Name = "certificates",
            DisplayName = "Certificates",
            ListObjectsEndpoint = "/v1/certificates",
        }.ToEntity();

        Assert.Equal("certificates", resource.Name);
        Assert.Equal("Certificates", resource.DisplayName);
        Assert.Equal("/v1/certificates", resource.ListObjectsEndpoint);
    }

    [Fact]
    public void ApplyTo_OverwritesEveryMappedField()
    {
        var resource = StoredResource();

        new ResourceRequestDto { Name = "groups", DisplayName = "Groups", ListObjectsEndpoint = null }.ApplyTo(resource);

        Assert.Equal("groups", resource.Name);
        Assert.Equal("Groups", resource.DisplayName);
        Assert.Null(resource.ListObjectsEndpoint);
    }

    [Fact]
    public void ListDto_CarriesTheIdentityAndFields()
    {
        var resource = StoredResource();

        var dto = resource.ToDto();

        Assert.Equal(resource.Uuid, dto.Uuid);
        Assert.Equal("certificates", dto.Name);
        Assert.Equal("Certificates", dto.DisplayName);
        Assert.Equal("/v1/certificates", dto.ListObjectsEndpoint);
    }

    [Theory]
    [InlineData("/v1/certificates", true)]
    [InlineData("", false)]
    [InlineData(null, false)]
    public void ObjectAccess_FollowsWhetherAListObjectsEndpointIsSet(string? endpoint, bool expected)
    {
        var resource = StoredResource();
        resource.ListObjectsEndpoint = endpoint;

        Assert.Equal(expected, resource.ToDto().ObjectAccess);
    }

    [Fact]
    public void DetailDto_ExpandsTheResourceActions()
    {
        var resource = StoredResource();
        resource.Actions = [new ActionEntity { Uuid = Guid.NewGuid(), Name = "list", DisplayName = "List" }];

        Assert.Equal("list", Assert.Single(resource.ToDetailDto().Actions).Name);
    }

    [Fact]
    public void DetailDto_SubstitutesAnEmptyActionListForAMissingOne()
    {
        var resource = StoredResource();
        resource.Actions = null!;

        Assert.Empty(resource.ToDetailDto().Actions);
    }

    [Fact]
    public void EntityMapper_BuildsAResourceFromACreateRequest()
    {
        Assert.Equal("certificates", ResourceEntityMapper.Instance.ToEntity(new ResourceRequestDto { Name = "certificates" }).Name);
    }

    [Fact]
    public void EntityMapper_RejectsACreateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceEntityMapper.Instance.ToEntity(new ActionRequestDto { Name = "list" }));

        Assert.Contains("ActionRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_AppliesTheSameRequestShapeOnUpdate()
    {
        var resource = StoredResource();

        ResourceEntityMapper.Instance.ApplyUpdate(new ResourceRequestDto { Name = "groups", DisplayName = "Groups" }, resource);

        Assert.Equal("groups", resource.Name);
    }

    [Fact]
    public void EntityMapper_RejectsAnUpdateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => ResourceEntityMapper.Instance.ApplyUpdate(new ActionRequestDto { Name = "list" }, StoredResource()));

        Assert.Contains("ActionRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_DelegatesBothResponseShapes()
    {
        var resource = StoredResource();

        Assert.Equal(resource.Uuid, ResourceEntityMapper.Instance.ToDto(resource).Uuid);
        Assert.Empty(ResourceEntityMapper.Instance.ToDetailDto(resource).Actions);
    }
}
