using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ObjectPermissionsRequestDto
    {
        [Required]
        public Guid Uuid { get; init; }

        [Required]
        public string Name { get; init; }

        public List<string>? Allow { get; init; }

        public List<string>? Deny { get; init; }

    }
}
