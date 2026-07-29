using Auth.Common.Models.Dto;
using Auth.Common.Services;
using Auth.Models.Dto;
using Auth.Models.Entities;

namespace Auth.Models.Mappings
{
    public static class RoleMapper
    {
        /// <summary>
        /// Builds the entity for a role create request. The requested permissions are not mapped here - they are saved
        /// separately once the role row exists.
        /// </summary>
        public static Role ToEntity(this RoleRequestDto dto)
        {
            return new Role
            {
                Name = dto.Name!,
                Description = dto.Description,
                Email = dto.Email,
                SystemRole = dto.SystemRole.GetValueOrDefault(),
            };
        }

        /// <summary>
        /// Copies the updatable members onto a loaded role. Name and SystemRole are not part of the update request and
        /// are left untouched.
        /// </summary>
        public static void ApplyTo(this RoleUpdateRequestDto dto, Role role)
        {
            role.Description = dto.Description;
            role.Email = dto.Email;
        }

        public static RoleDto ToDto(this Role role)
        {
            return new RoleDto
            {
                Uuid = role.Uuid,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Name = role.Name,
                Description = role.Description,
                Email = role.Email,
                SystemRole = role.SystemRole,
            };
        }

        public static RoleDetailDto ToDetailDto(this Role role)
        {
            return new RoleDetailDto
            {
                Uuid = role.Uuid,
                CreatedAt = role.CreatedAt,
                UpdatedAt = role.UpdatedAt,
                Name = role.Name,
                Description = role.Description,
                Email = role.Email,
                SystemRole = role.SystemRole,
                // Users is loaded through the repository's detail includes on every path reaching this mapper - see
                // UserMapper.ToDetailDto for the same reasoning.
                Users = role.Users?.Select(user => user.ToDto()).ToList() ?? [],
            };
        }
    }

    public sealed class RoleEntityMapper : IEntityMapper<Role, RoleDto, RoleDetailDto>
    {
        public static readonly RoleEntityMapper Instance = new();

        public Role ToEntity(ICrudRequestDto dto)
        {
            if (dto is not RoleRequestDto roleRequestDto) throw new ArgumentException($"Cannot create role from '{dto.GetType().Name}'.", nameof(dto));

            return RoleMapper.ToEntity(roleRequestDto);
        }

        public void ApplyUpdate(ICrudRequestDto dto, Role entity)
        {
            if (dto is not RoleUpdateRequestDto roleUpdateRequestDto) throw new ArgumentException($"Cannot update role from '{dto.GetType().Name}'.", nameof(dto));

            RoleMapper.ApplyTo(roleUpdateRequestDto, entity);
        }

        public RoleDto ToDto(Role entity) => RoleMapper.ToDto(entity);

        public RoleDetailDto ToDetailDto(Role entity) => RoleMapper.ToDetailDto(entity);
    }
}
