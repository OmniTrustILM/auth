using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record ActionDto : CrudResponseDto
    {
        [Required]
        public string Name { get; init; }

        [Required]
        public string DisplayName { get; init; }
    }
}
