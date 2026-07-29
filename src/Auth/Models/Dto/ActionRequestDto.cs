using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ActionRequestDto : ICrudRequestDto
    {
        [Required]
        public string? Name { get; init; }

        public string? DisplayName { get; init; }

    }
}
