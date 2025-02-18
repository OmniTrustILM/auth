namespace Czertainly.Auth.Models.Dto
{
    public record AuthenticationRequestDto
    {
        public string? CertificateContent { get; init; }

        public AuthenticationTokenClaimsDto? AuthenticationTokenUserClaims { get; init; }
        
        public string? SystemUsername { get; init; }
        public string? UserUuid { get; init; }

    }
}
