using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ResourcePermissionsRequestDto
    {
        [Required]
        public string Name { get; init; }

        [Required]
        public bool AllowAllActions { get; set; }

        public List<string>? Actions { get; init; }

        public List<ObjectPermissionsRequestDto>? Objects { get; init; }

    }
}
