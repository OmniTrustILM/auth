using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Services;

namespace Auth.Tests.TestSupport;

public sealed class FakeUserService : IUserService
{
    public PagedResponse<UserDto> Page { get; set; } = new() { Data = [], Links = new PagingMetadata() };
    public UserDetailDto Detail { get; set; } = new() { Username = "jane" };
    public AuthenticationResponseDto AuthenticationResponse { get; set; } = new() { Authenticated = true };
    public List<UserDto> RoleUsers { get; set; } = [];

    public List<IQueryRequestDto> Queries { get; } = [];
    public List<ICrudRequestDto> Created { get; } = [];
    public List<(Guid Key, ICrudRequestDto Dto)> Updated { get; } = [];
    public List<Guid> Deleted { get; } = [];
    public List<Guid> DetailsRequested { get; } = [];
    public List<AuthenticationRequestDto> Authenticated { get; } = [];
    public List<AuthenticationRequestDto> Identified { get; } = [];
    public List<(Guid UserUuid, bool Enable)> EnableCalls { get; } = [];
    public List<(Guid UserUuid, Guid RoleUuid)> RolesAssigned { get; } = [];
    public List<(Guid UserUuid, List<Guid> RoleUuids)> RoleSetsAssigned { get; } = [];
    public List<(Guid UserUuid, Guid RoleUuid)> RolesRemoved { get; } = [];
    public List<Guid> RoleUsersRequested { get; } = [];

    public Task<PagedResponse<UserDto>> GetAsync(IQueryRequestDto dto)
    {
        Queries.Add(dto);
        return Task.FromResult(Page);
    }

    public Task<UserDetailDto> CreateAsync(ICrudRequestDto dto)
    {
        Created.Add(dto);
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> GetDetailAsync(Guid key)
    {
        DetailsRequested.Add(key);
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto)
    {
        Updated.Add((key, dto));
        return Task.FromResult(Detail);
    }

    public Task DeleteAsync(Guid key)
    {
        Deleted.Add(key);
        return Task.CompletedTask;
    }

    public Task<AuthenticationResponseDto> AuthenticateUserAsync(AuthenticationRequestDto authenticationRequestDto)
    {
        Authenticated.Add(authenticationRequestDto);
        return Task.FromResult(AuthenticationResponse);
    }

    public Task<UserDetailDto> IdentifyUserAsync(AuthenticationRequestDto authenticationRequestDto)
    {
        Identified.Add(authenticationRequestDto);
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> EnableUserAsync(Guid userUuid, bool enableFlag)
    {
        EnableCalls.Add((userUuid, enableFlag));
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> AssignRoleAsync(Guid userUuid, Guid roleUuid)
    {
        RolesAssigned.Add((userUuid, roleUuid));
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> AssignRolesAsync(Guid userUuid, IEnumerable<Guid> roleUuids)
    {
        RoleSetsAssigned.Add((userUuid, roleUuids.ToList()));
        return Task.FromResult(Detail);
    }

    public Task<UserDetailDto> RemoveRoleAsync(Guid userUuid, Guid roleUuid)
    {
        RolesRemoved.Add((userUuid, roleUuid));
        return Task.FromResult(Detail);
    }

    public Task<List<UserDto>> GetRoleUsersAsync(Guid roleUuid)
    {
        RoleUsersRequested.Add(roleUuid);
        return Task.FromResult(RoleUsers);
    }
}

public sealed class FakeRoleService : IRoleService
{
    public PagedResponse<RoleDto> Page { get; set; } = new() { Data = [], Links = new PagingMetadata() };
    public RoleDetailDto Detail { get; set; } = new() { Name = "admin" };
    public List<RoleDto> UserRoles { get; set; } = [];

    public List<IQueryRequestDto> Queries { get; } = [];
    public List<ICrudRequestDto> Created { get; } = [];
    public List<(Guid Key, ICrudRequestDto Dto)> Updated { get; } = [];
    public List<Guid> Deleted { get; } = [];
    public List<Guid> DetailsRequested { get; } = [];
    public List<Guid> UserRolesRequested { get; } = [];
    public List<(Guid RoleUuid, List<Guid> UserUuids)> UsersAssigned { get; } = [];

    public Task<PagedResponse<RoleDto>> GetAsync(IQueryRequestDto dto)
    {
        Queries.Add(dto);
        return Task.FromResult(Page);
    }

    public Task<RoleDetailDto> CreateAsync(ICrudRequestDto dto)
    {
        Created.Add(dto);
        return Task.FromResult(Detail);
    }

    public Task<RoleDetailDto> GetDetailAsync(Guid key)
    {
        DetailsRequested.Add(key);
        return Task.FromResult(Detail);
    }

    public Task<RoleDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto)
    {
        Updated.Add((key, dto));
        return Task.FromResult(Detail);
    }

    public Task DeleteAsync(Guid key)
    {
        Deleted.Add(key);
        return Task.CompletedTask;
    }

    public Task<List<RoleDto>> GetUserRolesAsync(Guid userUuid)
    {
        UserRolesRequested.Add(userUuid);
        return Task.FromResult(UserRoles);
    }

    public Task<RoleDetailDto> AssignUsersAsync(Guid roleUuid, IEnumerable<Guid> userUuids)
    {
        UsersAssigned.Add((roleUuid, userUuids.ToList()));
        return Task.FromResult(Detail);
    }
}

public sealed class FakeResourceService : IResourceService
{
    public List<ResourceDetailDto> AllResources { get; set; } = [];
    public SyncResourcesResponseDto SyncResult { get; set; } = new();

    public List<List<ResourceSyncRequestDto>> Added { get; } = [];
    public List<List<ResourceSyncRequestDto>> Synced { get; } = [];
    public int AllResourcesRequested { get; private set; }

    public Task<List<ResourceDetailDto>> GetAllResourcesAsync()
    {
        AllResourcesRequested++;
        return Task.FromResult(AllResources);
    }

    public Task AddResourcesAsync(List<ResourceSyncRequestDto> resources)
    {
        Added.Add(resources);
        return Task.CompletedTask;
    }

    public Task<SyncResourcesResponseDto> SyncResourcesAsync(List<ResourceSyncRequestDto> resources)
    {
        Synced.Add(resources);
        return Task.FromResult(SyncResult);
    }

    public Task<PagedResponse<ResourceDto>> GetAsync(IQueryRequestDto dto) => throw new NotSupportedException();

    public Task<ResourceDetailDto> CreateAsync(ICrudRequestDto dto) => throw new NotSupportedException();

    public Task<ResourceDetailDto> GetDetailAsync(Guid key) => throw new NotSupportedException();

    public Task<ResourceDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto) => throw new NotSupportedException();

    public Task DeleteAsync(Guid key) => throw new NotSupportedException();
}

public sealed class FakeActionService : IActionService
{
    public PagedResponse<ActionDto> Page { get; set; } = new() { Data = [], Links = new PagingMetadata() };

    public List<IQueryRequestDto> Queries { get; } = [];

    public Task<PagedResponse<ActionDto>> GetAsync(IQueryRequestDto dto)
    {
        Queries.Add(dto);
        return Task.FromResult(Page);
    }

    public Task<ActionDto> CreateAsync(ICrudRequestDto dto) => throw new NotSupportedException();

    public Task<ActionDto> GetDetailAsync(Guid key) => throw new NotSupportedException();

    public Task<ActionDto> UpdateAsync(Guid key, ICrudRequestDto dto) => throw new NotSupportedException();

    public Task DeleteAsync(Guid key) => throw new NotSupportedException();
}
