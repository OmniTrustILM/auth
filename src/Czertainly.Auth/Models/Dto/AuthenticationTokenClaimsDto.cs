using Czertainly.Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Czertainly.Auth.Models.Dto
{
    public class AuthenticationTokenClaimsDto
    {
        [Required]
        [JsonPropertyName("sub")]
        public string SubjectId { get; init; }

        [JsonPropertyName("username")]
        public string? Username { get; init; }

        [JsonPropertyName("preferred_username")]
        public string? PreferredUsername { get; init; }

        [JsonPropertyName("given_name")]
        public string? FirstName { get; init; }

        [JsonPropertyName("family_name")]
        public string? LastName { get; init; }

        [JsonPropertyName("email")]
        public string? Email { get; init; }

        [JsonPropertyName("roles")]
        public string[] Roles { get; init; } = Array.Empty<string>();

        public bool Enabled { get; init; } = true;

    }
}
