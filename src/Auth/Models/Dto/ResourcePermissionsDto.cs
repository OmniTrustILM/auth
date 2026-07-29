using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ResourcePermissionsDto
    {
        [Required]
        public string Name { get; init; }

        [Required]
        public bool AllowAllActions { get; set; }

        [Required]
        public List<string> Actions { get; init; } = new List<string>();

        [Required]
        public List<ObjectPermissionsDto> Objects { get; init; } = new List<ObjectPermissionsDto>();

    }
}
