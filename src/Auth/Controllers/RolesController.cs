using Auth.Common.Filters;
using Auth.Common.Models.Dto;
using Auth.Models.Dto;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    [ApiVersion("1.0")]
    [Route("auth/roles")]
    [ApiController]
    public class RolesController : ControllerBase
    {
        private readonly IRoleService _roleService;

        public RolesController(IRoleService roleService)
        {
            _roleService = roleService;
        }

        [HttpGet]
        public async Task<ActionResult<PagedResponse<RoleDto>>> GetRolesAsync()
        {
            var result = await _roleService.GetAsync(new QueryRequestDto());

            return Ok(result);
        }

        [HttpPost]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<RoleDetailDto>> CreateRoleAsync([FromBody] RoleRequestDto roleRequestDto)
        {
            var result = await _roleService.CreateAsync(roleRequestDto);

            return Created("auth/roles", result);
        }

        [HttpGet("{roleUuid}")]
        public async Task<ActionResult<RoleDetailDto>> GetRoleAsync([FromRoute] Guid roleUuid)
        {
            var result = await _roleService.GetDetailAsync(roleUuid);

            return Ok(result);
        }

        [HttpPut("{roleUuid}")]
        [ServiceFilter(typeof(ValidationFilter))]
        public async Task<ActionResult<RoleDetailDto>> UpdateRoleAsync([FromRoute] Guid roleUuid, [FromBody] RoleUpdateRequestDto roleRequestDto)
        {
            var result = await _roleService.UpdateAsync(roleUuid, roleRequestDto);

            return Ok(result);
        }

        [HttpDelete("{roleUuid}")]
        public async Task<ActionResult> DeleteRoleAsync([FromRoute] Guid roleUuid)
        {
            await _roleService.DeleteAsync(roleUuid);

            return NoContent();
        }

        [HttpGet("{roleUuid}/users")]
        public async Task<ActionResult<List<UserDto>>> GetRoleUsersAsync([FromServices] IUserService userService, [FromRoute] Guid roleUuid)
        {
            var result = await userService.GetRoleUsersAsync(roleUuid);

            return Ok(result);
        }

        [HttpPatch("{roleUuid}/users")]
        public async Task<ActionResult<RoleDetailDto>> AssignRoleUsersAsync([FromRoute] Guid roleUuid, [FromBody] IEnumerable<Guid> userUuids)
        {
            var result = await _roleService.AssignUsersAsync(roleUuid, userUuids);

            return Ok(result);
        }

    }
}
