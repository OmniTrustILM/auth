using Auth.Common.Models.Entities;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Models.Entities;

[Table("resource")]
[Index(nameof(Name), IsUnique = true)]
public class Resource : BaseEntity
{
    [Required]
    [Column("name")]
    public string Name { get; set; }

    [Required]
    [Column("display_name")]
    public string DisplayName { get; set; }

    [Column("list_objects_endpoint")]
    public string? ListObjectsEndpoint { get; set; }

    public ICollection<Action> Actions { get; set; }
    public ICollection<Permission> Permissions { get; set; }
}
