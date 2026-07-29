using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record RoleDto : CrudTimestampedResponseDto
    {
        [Required]
        public string Name { get; init; }

        public string? Description { get; init; }

        public string? Email { get; init; }

        [Required]
        public bool SystemRole { get; init; }

    }
}
