using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record UserDto : CrudTimestampedResponseDto
    {
        [Required]
        public string Username { get; init; }

        public string? FirstName { get; init; }

        public string? LastName { get; init; }

        public string? Email { get; init; }

        public string? Description { get; init; }

        [Required]
        public List<NameAndUuidDto> Groups { get; init; } = new List<NameAndUuidDto>();

        [Required]
        public bool Enabled { get; init; }

        [Required]
        public bool SystemUser { get; init; }
    }
}
