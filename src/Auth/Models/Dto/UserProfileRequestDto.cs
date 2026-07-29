using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Auth.Models.Dto
{
    public record UserProfileRequestDto : ICrudRequestDto
    {
        [Required]
        [JsonPropertyName("X-APP-CERTIFICATE")]
        public string? ClientCertificate { get; init; }

    }
}
