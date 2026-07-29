using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Auth.Common.Models.Entities
{
    public abstract class TimestampedEntity : BaseEntity
    {
        [Required]
        [Column("created_at")]
        public DateTimeOffset CreatedAt { get; set; }

        [Required]
        [Column("updated_at")]
        public DateTimeOffset UpdatedAt { get; set; }
    }
}
