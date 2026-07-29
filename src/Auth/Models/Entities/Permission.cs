using Auth.Common.Models.Entities;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Models.Entities;

[Table("permission")]
public class Permission : BaseEntity
{
    [Required]
    [Column("role_uuid")]
    public Guid RoleUuid { get; set; }

    [ForeignKey(nameof(RoleUuid))]
    public Role Role { get; set; }

    [Column("resource_uuid")]
    public Guid? ResourceUuid { get; set; }

    [ForeignKey(nameof(ResourceUuid))]
    public Resource? Resource { get; set; }

    [Column("action_uuid")]
    public Guid? ActionUuid { get; set; }

    [ForeignKey(nameof(ActionUuid))]
    public Action? Action { get; set; }

    [Column("object_uuid")]
    public Guid? ObjectUuid { get; set; }

    [Column("object_name")]
    public string? ObjectName { get; set; }

    [Required]
    [Column("is_allowed")]
    public bool IsAllowed { get; set; }

}
