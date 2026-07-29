using Auth.Common.Services;
using Auth.Models.Dto;

namespace Auth.Services
{
    public interface IUserService : ICrudService<UserDto, UserDetailDto>
    {
        Task<AuthenticationResponseDto> AuthenticateUserAsync(AuthenticationRequestDto authenticationRequestDto);
        Task<UserDetailDto> IdentifyUserAsync(AuthenticationRequestDto authenticationRequestDto);


        Task<UserDetailDto> EnableUserAsync(Guid userUuid, bool enableFlag);

        Task<UserDetailDto> AssignRoleAsync(Guid userUuid, Guid roleUuid);
        Task<UserDetailDto> AssignRolesAsync(Guid userUuid, IEnumerable<Guid> roleUuids);
        Task<UserDetailDto> RemoveRoleAsync(Guid userUuid, Guid roleUuid);

        Task<List<UserDto>> GetRoleUsersAsync(Guid roleUuid);
    }
}
