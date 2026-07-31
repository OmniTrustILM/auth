using Auth.Models.Dto;
using Auth.Models.Mappings;
using ActionEntity = Auth.Models.Entities.Action;

namespace Auth.Tests.Mappings;

public class ActionMapperTests
{
    [Fact]
    public void CreateRequest_BecomesAnAction()
    {
        var action = new ActionRequestDto { Name = "list", DisplayName = "List" }.ToEntity();

        Assert.Equal("list", action.Name);
        Assert.Equal("List", action.DisplayName);
    }

    [Fact]
    public void ApplyTo_OverwritesBothFields()
    {
        var action = new ActionEntity { Name = "list", DisplayName = "List" };

        new ActionRequestDto { Name = "detail", DisplayName = "Detail" }.ApplyTo(action);

        Assert.Equal("detail", action.Name);
        Assert.Equal("Detail", action.DisplayName);
    }

    [Fact]
    public void Dto_CarriesTheIdentityAndFields()
    {
        var action = new ActionEntity { Uuid = Guid.NewGuid(), Name = "list", DisplayName = "List" };

        var dto = action.ToDto();

        Assert.Equal(action.Uuid, dto.Uuid);
        Assert.Equal("list", dto.Name);
        Assert.Equal("List", dto.DisplayName);
    }

    [Fact]
    public void EntityMapper_BuildsAnActionFromACreateRequest()
    {
        Assert.Equal("list", ActionEntityMapper.Instance.ToEntity(new ActionRequestDto { Name = "list" }).Name);
    }

    [Fact]
    public void EntityMapper_RejectsACreateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => ActionEntityMapper.Instance.ToEntity(new ResourceRequestDto { Name = "certificates" }));

        Assert.Contains("ResourceRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_AppliesTheSameRequestShapeOnUpdate()
    {
        var action = new ActionEntity { Name = "list", DisplayName = "List" };

        ActionEntityMapper.Instance.ApplyUpdate(new ActionRequestDto { Name = "detail", DisplayName = "Detail" }, action);

        Assert.Equal("detail", action.Name);
    }

    [Fact]
    public void EntityMapper_RejectsAnUpdateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(
            () => ActionEntityMapper.Instance.ApplyUpdate(new ResourceRequestDto { Name = "certificates" }, new ActionEntity()));

        Assert.Contains("ResourceRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_ReportsTheSameShapeForTheListAndDetailResponses()
    {
        var action = new ActionEntity { Uuid = Guid.NewGuid(), Name = "list", DisplayName = "List" };

        Assert.Equal(ActionEntityMapper.Instance.ToDto(action), ActionEntityMapper.Instance.ToDetailDto(action));
    }
}
