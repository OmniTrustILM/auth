using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Auth.Models.Dto
{
    public record UserDetailDto : UserDto
    {
        [JsonPropertyOrder(98)]
        public UserCertificateDto? Certificate { get; init; }

        [Required]
        [JsonPropertyOrder(99)]
        public List<RoleDto> Roles { get; init; } = new List<RoleDto>();

    }
}
