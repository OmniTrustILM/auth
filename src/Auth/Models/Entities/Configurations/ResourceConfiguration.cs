using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Models.Entities.Configurations
{
    public class ResourceConfiguration : IEntityTypeConfiguration<Resource>
    {
        public void Configure(EntityTypeBuilder<Resource> builder)
        {
            builder
                .HasMany(r => r.Actions)
                .WithMany(a => a.Resources)
                .UsingEntity<Dictionary<string, object>>(
                    "resource_action",
                    x => x.HasOne<Action>().WithMany().HasForeignKey("action_uuid").OnDelete(DeleteBehavior.Cascade),
                    x => x.HasOne<Resource>().WithMany().HasForeignKey("resource_uuid").OnDelete(DeleteBehavior.Cascade));
        }
    }
}
