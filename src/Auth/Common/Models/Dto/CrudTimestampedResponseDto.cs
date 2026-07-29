using Microsoft.AspNetCore.Mvc;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Auth.Common.Models.Dto
{
    public record CrudTimestampedResponseDto : CrudResponseDto
    {
        [Required]
        [JsonPropertyOrder(99)]
        public DateTimeOffset CreatedAt { get; init; }

        [Required]
        [JsonPropertyOrder(100)]
        public DateTimeOffset UpdatedAt { get; init; }
    }
}
