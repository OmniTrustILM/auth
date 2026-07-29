using Czertainly.Auth.Common.Exceptions;
using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Data.Contracts;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Entities;
using Czertainly.Auth.Models.Mappings;

namespace Czertainly.Auth.Services
{
    public class RoleService : CrudService<Role, RoleDto, RoleDetailDto>, IRoleService
    {
        private readonly IPermissionService _permissionService;

        public RoleService(IRepositoryManager repositoryManager, ILogger<RoleService> logger, IPermissionService permissionService)
            : base(repositoryManager, repositoryManager.Role, RoleEntityMapper.Instance, logger)
        {
            _permissionService = permissionService;
        }

        public override async Task<RoleDetailDto> CreateAsync(ICrudRequestDto dto)
        {
            var roleRequestDto = dto as RoleRequestDto;
            if(roleRequestDto == null) throw new InvalidActionException("Cannot create role. Invalid DTO");

            // check uniqueness of role
            var checkedRole = await _repository.GetByConditionAsync(r => r.Name == roleRequestDto.Name);
            if (checkedRole != null) throw new EntityNotUniqueException($"Role with name '{roleRequestDto.Name}' already exists");

            var newRole = await base.CreateAsync(dto);
            if (roleRequestDto.Permissions != null) await _permissionService.SaveRolePermissionsAsync(newRole.Uuid, roleRequestDto.Permissions);

            return newRole;
        }

        public override async Task<RoleDetailDto> UpdateAsync(Guid key, ICrudRequestDto dto)
        {
            var role = await _repository.GetByKeyAsync(key);
            if (role.SystemRole) throw new InvalidActionException("Cannot update system role.");

            return await base.UpdateAsync(key, dto);
        }

        public override async Task DeleteAsync(Guid key)
        {
            var role = await _repository.GetByKeyAsync(key);
            if (role.SystemRole) throw new InvalidActionException("Cannot delete system role.");

            await base.DeleteAsync(key);
        }

        public async Task<List<RoleDto>> GetUserRolesAsync(Guid userUuid)
        {
            var roles = await _repositoryManager.Role.GetUserRolesAsync(userUuid);
            return roles.Select(role => role.ToDto()).ToList();
        }

        public async Task<RoleDetailDto> AssignUsersAsync(Guid roleUuid, IEnumerable<Guid> userUuids)
        {
            var role = await _repository.GetByKeyAsync(roleUuid);
            var users = await _repositoryManager.User.GetByUuidsAsync(userUuids);

            role.Users.Clear();
            foreach (var user in users) role.Users.Add(user);
            await _repositoryManager.SaveAsync();

            return role.ToDetailDto();
        }
    }
}
