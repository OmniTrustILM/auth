using Auth.Common.Models.Dto;
using System.ComponentModel.DataAnnotations;

namespace Auth.Models.Dto
{
    public record PermissionRequestDto
    {
        public Guid? ResourceUuid { get; init; }
        
        public Guid? ActionUuid { get; init; }
        
        [Required]
        public bool IsAllowed { get; init; }

    }
}
