using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Auth.Models.Dto
{
    public record ResourceDto : CrudResponseDto
    {
        [Required]
        public string Name { get; init; }
        
        [Required]
        public string DisplayName { get; init; }

        [Required]
        public bool ObjectAccess { get { return !string.IsNullOrEmpty(ListObjectsEndpoint); } }

        public string? ListObjectsEndpoint { get; init; }

    }
}
