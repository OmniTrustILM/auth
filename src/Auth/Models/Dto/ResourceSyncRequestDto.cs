using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ResourceSyncRequestDto : ICrudRequestDto
    {
        [Required]
        public string Name { get; init; }

        public string? ListObjectsEndpoint { get; init; }

        [Required]
        public List<string> Actions { get; init; } = new List<string>();
    }
}
