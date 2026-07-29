using Auth.Common.Models.Dto;
using Auth.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Models.Entities;

[Table("user")]
[Index(nameof(Username), IsUnique = true)]
public class User : TimestampedEntity
{
    [Required]
    [Column("username")]
    public string? Username { get; set; }

    [Column("first_name")]
    public string? FirstName { get; set; }

    [Column("last_name")]
    public string? LastName { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("groups", TypeName = "text")]
    public List<NameAndUuidDto>? Groups { get; set; } = new List<NameAndUuidDto>();

    [Required]
    [Column("enabled")]
    public bool Enabled { get; set; }

    [Required]
    [Column("is_system_user")]
    public bool SystemUser { get; set; }

    [Column("certificate_uuid")]
    public Guid? CertificateUuid { get; set; }

    [Column("certificate_fingerprint")]
    public string? CertificateFingerprint { get; set; }

    [Column("auth_token_sub")]
    public string? AuthTokenSubjectId { get; set; }

    public ICollection<Role> Roles { get; set; } = null!;

}
