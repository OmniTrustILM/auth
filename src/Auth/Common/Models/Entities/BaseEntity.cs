using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Common.Models.Entities
{
    public abstract class BaseEntity : IBaseEntity
    {
        [Key]
        [Column("uuid", Order = 0)]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Uuid { get; set; } = Guid.NewGuid();
    }
}
