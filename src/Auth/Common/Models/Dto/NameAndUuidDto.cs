using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace Auth.Common.Models.Dto
{
    public record NameAndUuidDto : CrudResponseDto
    {
        [Required]
        public string Name { get; init; }
    }
}
