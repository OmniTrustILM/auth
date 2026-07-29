using Auth.Common.Filters;
using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    [ApiVersion("1.0")]
    [Route("auth")]
    [ApiController]
    public class UsersController : ControllerBase
    {
        private readonly IUserService _userService;

        public UsersController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpPost("")]
        public async Task<ActionResult<AuthenticationResponseDto>> AuthenticateUserAsync([FromBody] AuthenticationRequestDto authenticationRequestDto)
        {
            var result = await _userService.AuthenticateUserAsync(authenticationRequestDto);

            return Ok(result);
        }

        [HttpPost("users/identify")]
        public async Task<ActionResult<UserDetailDto>> IdentifyUserAsync([FromBody] AuthenticationRequestDto authenticationRequestDto)
        {
            var result = await _userService.IdentifyUserAsync(authenticationRequestDto);

            return Ok(result);
        }

        [HttpGet("users")]

        public async Task<ActionResult<PagedResponse<UserDto>>> GetUsersAsync([FromQuery] string? group)
        //public async Task<ActionResult<PagedResponse<UserDto>>> GetUsersAsync()
        {
            var result = await _userService.GetAsync(new UserQueryRequestDto { Group = group });

            return Ok(result);
        }

        [HttpPost("users")]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<UserDetailDto>> CreateUserAsync([FromBody] UserRequestDto userRequestDto)
        {
            var result = await _userService.CreateAsync(userRequestDto);

            return Created("auth/users", result);
        }

        [HttpGet("users/{userUuid}")]
        public async Task<ActionResult<UserDetailDto>> GetUserAsync([FromRoute] Guid userUuid)
        {
            var result = await _userService.GetDetailAsync(userUuid);

            return Ok(result);
        }

        [HttpPut("users/{userUuid}")]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<UserDetailDto>> UpdateUserAsync([FromRoute] Guid userUuid, [FromBody] UserUpdateRequestDto userRequestDto)
        {
            var result = await _userService.UpdateAsync(userUuid, userRequestDto);

            return Ok(result);
        }

        [HttpDelete("users/{userUuid}")]
        public async Task<ActionResult> DeleteUserAsync([FromRoute] Guid userUuid)
        {
            await _userService.DeleteAsync(userUuid);

            return NoContent();
        }

        [HttpPatch("users/{userUuid}/enable")]
        public async Task<ActionResult<UserDetailDto>> EnableUserAsync([FromRoute] Guid userUuid)
        {
            var result = await _userService.EnableUserAsync(userUuid, true);

            return Ok(result);
        }

        [HttpPatch("users/{userUuid}/disable")]
        public async Task<ActionResult<UserDetailDto>> DisableUserAsync([FromRoute] Guid userUuid)
        {
            var result = await _userService.EnableUserAsync(userUuid, false);

            return Ok(result);
        }

        [HttpGet("users/{userUuid}/roles")]
        public async Task<ActionResult<List<RoleDto>>> GetUserRolesAsync([FromServices] IRoleService roleService, [FromRoute] Guid userUuid)
        {
            var result = await roleService.GetUserRolesAsync(userUuid);

            return Ok(result);
        }

        [HttpPatch("users/{userUuid}/roles")]
        public async Task<ActionResult<UserDetailDto>> AssignRolesAsync([FromRoute] Guid userUuid, [FromBody] IEnumerable<Guid> roleUuids)
        {
            var result = await _userService.AssignRolesAsync(userUuid, roleUuids);

            return Ok(result);
        }

        [HttpPut("users/{userUuid}/roles/{roleUuid}")]
        public async Task<ActionResult<UserDetailDto>> AssignRoleAsync([FromRoute] Guid userUuid, [FromRoute] Guid roleUuid)
        {
            var result = await _userService.AssignRoleAsync(userUuid, roleUuid);

            return Ok(result);
        }

        [HttpDelete("users/{userUuid}/roles/{roleUuid}")]
        public async Task<ActionResult<UserDetailDto>> RemoveRoleAsync([FromRoute] Guid userUuid, [FromRoute] Guid roleUuid)
        {
            var result = await _userService.RemoveRoleAsync(userUuid, roleUuid);

            return Ok(result);
        }
    }
}
