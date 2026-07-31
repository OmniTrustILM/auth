using Auth.Models.Dto;
using Auth.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace Auth.Controllers
{
    [ApiVersion("1.0")]
    [Route("auth")]
    [ApiController]
    public class PermissionsController : ControllerBase
    {
        private readonly IPermissionService _permissionService;

        public PermissionsController(IPermissionService permissionService)
        {
            _permissionService = permissionService;
        }

        [HttpGet("roles/{roleUuid}/permissions")]
        public async Task<ActionResult<SubjectPermissionsDto>> GetRolePermissions([FromRoute] Guid roleUuid)
        {
            var result = await _permissionService.GetRolePermissionsAsync(roleUuid);

            return Ok(result);
        }

        [HttpGet("roles/{roleUuid}/permissions/{resourceUuid}")]
        public async Task<ActionResult<ResourcePermissionsDto>> GetRoleResourcePermissions([FromRoute] Guid roleUuid, [FromRoute] Guid resourceUuid)
        {
            var result = await _permissionService.GetRoleResourcesPermissionsAsync(roleUuid, resourceUuid);

            return Ok(result);
        }

        [HttpPost("roles/{roleUuid}/permissions")]
        public async Task<ActionResult<SubjectPermissionsDto>> SaveRolePermissions([FromRoute] Guid roleUuid, [FromBody] RolePermissionsRequestDto rolePermissions)
        {
            var result = await _permissionService.SaveRolePermissionsAsync(roleUuid, rolePermissions);

            return Ok(result);
        }

        [HttpGet("roles/{roleUuid}/permissions/{resourceUuid}/objects")]
        public async Task<ActionResult<List<ObjectPermissionsDto>>> GetRoleObjectsPermissions([FromRoute] Guid roleUuid, [FromRoute] Guid resourceUuid)
        {
            var result = await _permissionService.GetRoleObjectsPermissionsAsync(roleUuid, resourceUuid);

            return Ok(result);
        }

        [HttpPost("roles/{roleUuid}/permissions/{resourceUuid}/objects")]
        public async Task<ActionResult> SaveRoleObjectsPermissions([FromRoute] Guid roleUuid, [FromRoute] Guid resourceUuid, List<ObjectPermissionsRequestDto> objectsPermissions)
        {
            await _permissionService.SaveRoleObjectsPermissionsAsync(roleUuid, resourceUuid, objectsPermissions);

            return NoContent();
        }

        [HttpPut("roles/{roleUuid}/permissions/{resourceUuid}/objects/{objectUuid}")]
        public async Task<ActionResult> SaveRoleObjectPermissions([FromRoute] Guid roleUuid, [FromRoute] Guid resourceUuid, [FromRoute] Guid objectUuid, ObjectPermissionsRequestDto objectPermissions)
        {
            await _permissionService.SaveRoleObjectPermissionsAsync(roleUuid, resourceUuid, objectUuid, objectPermissions);

            return NoContent();
        }

        [HttpDelete("roles/{roleUuid}/permissions/{resourceUuid}/objects/{objectUuid}")]
        public async Task<ActionResult> DeleteRoleObjectPermissions([FromRoute] Guid roleUuid, [FromRoute] Guid resourceUuid, [FromRoute] Guid objectUuid)
        {
            await _permissionService.DeleteRoleObjectPermissionsAsync(roleUuid, resourceUuid, objectUuid);

            return NoContent();
        }


        [HttpGet("users/{userUuid}/permissions")]
        public async Task<ActionResult<SubjectPermissionsDto>> GetUserPermissionsAsync([FromRoute] Guid userUuid)
        {
            var result = await _permissionService.GetUserPermissionsAsync(userUuid);

            return Ok(result);
        }
    }
}
