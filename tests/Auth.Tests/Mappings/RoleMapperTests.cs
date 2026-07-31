using Auth.Models.Dto;
using Auth.Models.Entities;
using Auth.Models.Mappings;

namespace Auth.Tests.Mappings;

public class RoleMapperTests
{
    private static Role StoredRole() => new()
    {
        Uuid = Guid.NewGuid(),
        CreatedAt = new DateTimeOffset(2026, 1, 2, 3, 4, 5, TimeSpan.Zero),
        UpdatedAt = new DateTimeOffset(2026, 2, 3, 4, 5, 6, TimeSpan.Zero),
        Name = "admin",
        Description = "administrators",
        Email = "admin@example.test",
        SystemRole = true,
        Users = [],
    };

    [Fact]
    public void CreateRequest_BecomesARoleWithoutItsRequestedPermissions()
    {
        var dto = new RoleRequestDto
        {
            Name = "admin",
            Description = "administrators",
            Email = "admin@example.test",
            SystemRole = true,
            Permissions = new RolePermissionsRequestDto { AllowAllResources = true },
        };

        var role = dto.ToEntity();

        Assert.Equal("admin", role.Name);
        Assert.Equal("administrators", role.Description);
        Assert.Equal("admin@example.test", role.Email);
        Assert.True(role.SystemRole);
        Assert.Null(role.Permissions);
    }

    [Fact]
    public void CreateRequest_TreatsAnOmittedSystemFlagAsFalse()
    {
        Assert.False(new RoleRequestDto { Name = "admin", SystemRole = null }.ToEntity().SystemRole);
    }

    [Fact]
    public void UpdateRequest_OverwritesOnlyTheDescriptionAndEmail()
    {
        var role = StoredRole();

        new RoleUpdateRequestDto { Description = "changed", Email = "new@example.test" }.ApplyTo(role);

        Assert.Equal("changed", role.Description);
        Assert.Equal("new@example.test", role.Email);
        Assert.Equal("admin", role.Name);
        Assert.True(role.SystemRole);
    }

    [Fact]
    public void ListDto_CarriesTheIdentityTimestampsAndFields()
    {
        var role = StoredRole();

        var dto = role.ToDto();

        Assert.Equal(role.Uuid, dto.Uuid);
        Assert.Equal(role.CreatedAt, dto.CreatedAt);
        Assert.Equal(role.UpdatedAt, dto.UpdatedAt);
        Assert.Equal("admin", dto.Name);
        Assert.Equal("administrators", dto.Description);
        Assert.Equal("admin@example.test", dto.Email);
        Assert.True(dto.SystemRole);
    }

    [Fact]
    public void DetailDto_ExpandsTheAssignedUsers()
    {
        var role = StoredRole();
        role.Users = [new User { Uuid = Guid.NewGuid(), Username = "jane" }];

        Assert.Equal("jane", Assert.Single(role.ToDetailDto().Users).Username);
    }

    [Fact]
    public void DetailDto_SubstitutesAnEmptyUserListForAMissingOne()
    {
        var role = StoredRole();
        role.Users = null!;

        Assert.Empty(role.ToDetailDto().Users);
    }

    [Fact]
    public void EntityMapper_BuildsARoleFromACreateRequest()
    {
        Assert.Equal("admin", RoleEntityMapper.Instance.ToEntity(new RoleRequestDto { Name = "admin" }).Name);
    }

    [Fact]
    public void EntityMapper_RejectsACreateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => RoleEntityMapper.Instance.ToEntity(new UserRequestDto { Username = "jane" }));

        Assert.Contains("UserRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_AppliesAnUpdateRequestToALoadedRole()
    {
        var role = StoredRole();

        RoleEntityMapper.Instance.ApplyUpdate(new RoleUpdateRequestDto { Description = "changed" }, role);

        Assert.Equal("changed", role.Description);
    }

    [Fact]
    public void EntityMapper_RejectsAnUpdateRequestOfTheWrongType()
    {
        var exception = Assert.Throws<ArgumentException>(() => RoleEntityMapper.Instance.ApplyUpdate(new RoleRequestDto { Name = "admin" }, StoredRole()));

        Assert.Contains("RoleRequestDto", exception.Message);
    }

    [Fact]
    public void EntityMapper_DelegatesBothResponseShapes()
    {
        var role = StoredRole();

        Assert.Equal(role.Uuid, RoleEntityMapper.Instance.ToDto(role).Uuid);
        Assert.Empty(RoleEntityMapper.Instance.ToDetailDto(role).Users);
    }
}
