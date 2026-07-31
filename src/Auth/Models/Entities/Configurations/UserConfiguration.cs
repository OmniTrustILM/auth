using Auth.Common.Models.Dto;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System.Linq;

namespace Auth.Models.Entities.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            builder
                .HasMany(u => u.Roles)
                .WithMany(r => r.Users)
                .UsingEntity<Dictionary<string, object>>(
                    "user_role",
                    x => x.HasOne<Role>().WithMany().HasForeignKey("role_uuid").OnDelete(DeleteBehavior.Cascade),
                    x => x.HasOne<User>().WithMany().HasForeignKey("user_uuid").OnDelete(DeleteBehavior.Cascade));

            builder.Property(u => u.Groups)
                .HasConversion(
                    // An empty list is stored as an empty string rather than as null, because a null column value
                    // bypasses this conversion on the way back and would surface as no list at all instead of an
                    // empty one.
                    groups => groups == null ? String.Empty : String.Join("|", groups.Select(g => $"{g.Uuid}:{g.Name}").ToArray()),
                    v => String.IsNullOrEmpty(v) ? new List<NameAndUuidDto>() : v.Split("|", StringSplitOptions.RemoveEmptyEntries).Select(g => new NameAndUuidDto { Uuid = Guid.Parse(g.Substring(0, g.IndexOf(":"))), Name = g.Substring(g.IndexOf(":") + 1) }).ToList(),
                    new ValueComparer<List<NameAndUuidDto>>(
                        (c1, c2) => c1 == null ? c2 == null : c2 != null && c1.SequenceEqual(c2),
                        c => c == null ? 0 : c.Aggregate(0, (a, v) => HashCode.Combine(a, v.GetHashCode())),
                        c => c == null ? null! : c.ToList()));


        }
    }
}
