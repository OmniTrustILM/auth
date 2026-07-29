using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record UserProfileDto
    {
        [Required]
        public UserDto User { get; init; }

        [Required]
        public List<NameAndUuidDto> Roles { get; init; } = new List<NameAndUuidDto>();

        [Required]
        public SubjectPermissionsDto Permissions { get; init; }
    }
}
