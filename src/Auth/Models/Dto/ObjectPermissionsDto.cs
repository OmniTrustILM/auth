using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ObjectPermissionsDto
    {
        [Required]
        public Guid Uuid { get; init; }

        [Required]
        public string Name { get; init; }

        [Required]
        public List<string> Allow { get; init; } = new List<string>();

        [Required]
        public List<string> Deny { get; init; } = new List<string>();

    }
}
