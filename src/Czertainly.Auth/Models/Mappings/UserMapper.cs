using Czertainly.Auth.Common.Models.Dto;
using Czertainly.Auth.Common.Services;
using Czertainly.Auth.Models.Dto;
using Czertainly.Auth.Models.Entities;

namespace Czertainly.Auth.Models.Mappings
{
    public static class UserMapper
    {
        /// <summary>
        /// Builds the entity for a user auto-registered from an authentication token. Roles are deliberately started as
        /// an empty collection instead of being derived from the token role names, because the roles are resolved and
        /// assigned separately after the user row exists.
        /// </summary>
        public static User ToEntity(this AuthenticationTokenClaimsDto claims)
        {
            return new User
            {
                Username = claims.Username,
                FirstName = claims.FirstName,
                LastName = claims.LastName,
                Email = claims.Email,
                Enabled = claims.Enabled,
                AuthTokenSubjectId = claims.SubjectId,
                Roles = new List<Role>(),
            };
        }

        public static User ToEntity(this UserRequestDto dto)
        {
            return new User
            {
                Username = dto.Username,
                FirstName = dto.FirstName,
                LastName = dto.LastName,
                Email = dto.Email,
                Description = dto.Description,
                Groups = dto.Groups,
                Enabled = dto.Enabled.GetValueOrDefault(),
                SystemUser = dto.SystemUser.GetValueOrDefault(),
                CertificateUuid = dto.CertificateUuid,
                CertificateFingerprint = dto.CertificateFingerprint,
            };
        }

        /// <summary>
        /// Copies the updatable members onto a loaded user. Username, Enabled and SystemUser are not part of the update
        /// request and are left untouched.
        /// </summary>
        public static void ApplyTo(this UserUpdateRequestDto dto, User user)
        {
            user.FirstName = dto.FirstName;
            user.LastName = dto.LastName;
            user.Email = dto.Email;
            user.Description = dto.Description;
            user.Groups = dto.Groups;
            user.CertificateUuid = dto.CertificateUuid;
            user.CertificateFingerprint = dto.CertificateFingerprint;
        }

        public static UserDto ToDto(this User user)
        {
            return new UserDto
            {
                Uuid = user.Uuid,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Description = user.Description,
                Groups = user.Groups ?? new List<NameAndUuidDto>(),
                Enabled = user.Enabled,
                SystemUser = user.SystemUser,
            };
        }

        public static UserDetailDto ToDetailDto(this User user)
        {
            return new UserDetailDto
            {
                Uuid = user.Uuid,
                CreatedAt = user.CreatedAt,
                UpdatedAt = user.UpdatedAt,
                Username = user.Username!,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Email = user.Email,
                Description = user.Description,
                Groups = user.Groups ?? new List<NameAndUuidDto>(),
                Enabled = user.Enabled,
                SystemUser = user.SystemUser,
                Certificate = user.CertificateFingerprint == null
                    ? null
                    : new UserCertificateDto { Uuid = user.CertificateUuid, Fingerprint = user.CertificateFingerprint },
                // Every path reaching this mapper loads Roles through the repository's detail includes, so the coalesce
                // is unreachable; it keeps the [Required], non-nullable DTO property honest.
                Roles = user.Roles?.Select(role => role.ToDto()).ToList() ?? [],
            };
        }
    }

    public sealed class UserEntityMapper : IEntityMapper<User, UserDto, UserDetailDto>
    {
        public static readonly UserEntityMapper Instance = new();

        public User ToEntity(ICrudRequestDto dto)
        {
            if (dto is not UserRequestDto userRequestDto) throw new ArgumentException($"Cannot create user from '{dto.GetType().Name}'.", nameof(dto));

            return UserMapper.ToEntity(userRequestDto);
        }

        public void ApplyUpdate(ICrudRequestDto dto, User entity)
        {
            if (dto is not UserUpdateRequestDto userUpdateRequestDto) throw new ArgumentException($"Cannot update user from '{dto.GetType().Name}'.", nameof(dto));

            UserMapper.ApplyTo(userUpdateRequestDto, entity);
        }

        public UserDto ToDto(User entity) => UserMapper.ToDto(entity);

        public UserDetailDto ToDetailDto(User entity) => UserMapper.ToDetailDto(entity);
    }
}
