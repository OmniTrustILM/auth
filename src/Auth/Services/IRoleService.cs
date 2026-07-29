using Auth.Common.Data;
using Auth.Common.Services;
using Auth.Models.Dto;

namespace Auth.Services
{
    public interface IRoleService : ICrudService<RoleDto, RoleDetailDto>
    {
        Task<List<RoleDto>> GetUserRolesAsync(Guid userUuid);

        Task<RoleDetailDto> AssignUsersAsync(Guid roleUuid, IEnumerable<Guid> userUuids);

    }
}
