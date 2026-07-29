using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record RoleRequestDto : ICrudRequestDto
    {
        [Required]
        public string? Name { get; init; }

        public string? Description { get; init; }

        [EmailAddress]
        public string? Email { get; init; }

        public bool? SystemRole { get; init; }

        public RolePermissionsRequestDto? Permissions { get; init; }

    }
}
