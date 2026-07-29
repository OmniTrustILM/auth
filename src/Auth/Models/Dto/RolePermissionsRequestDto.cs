using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record RolePermissionsRequestDto
    {
        [Required]
        public bool AllowAllResources { get; set; }

        public List<ResourcePermissionsRequestDto>? Resources { get; init; }
    }
}