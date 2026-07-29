using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record RoleUpdateRequestDto : ICrudRequestDto
    {
        public string? Description { get; init; }

        [EmailAddress]
        public string? Email { get; init; }

    }
}
