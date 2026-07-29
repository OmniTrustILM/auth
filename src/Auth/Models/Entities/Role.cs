using Auth.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Models.Entities;

[Table("role")]
[Index(nameof(Name), IsUnique = true)]
public class Role : TimestampedEntity
{
    [Required]
    [Column("name")]
    public string Name { get; set; }

    [Column("description")]
    public string? Description { get; set; }

    [Column("email")]
    public string? Email { get; set; }

    [Required]
    [Column("system_role")]
    public bool SystemRole { get; set; }

    public ICollection<User> Users { get; set; }

    public ICollection<Permission> Permissions { get; set; }
}
