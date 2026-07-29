using Auth.Common.Models.Dto;

namespace Auth.Models.Dto
{
    public record UserCertificateDto
    {
        public Guid? Uuid { get; init; }
        public string? Fingerprint { get; init; }
    }
}
