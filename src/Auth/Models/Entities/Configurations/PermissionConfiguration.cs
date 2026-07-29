using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Auth.Models.Entities.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            builder
                .HasOne<Resource>(p => p.Resource)
                .WithMany(r => r.Permissions)
                .HasForeignKey(p => p.ResourceUuid)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientCascade);

            builder
                .HasOne<Action>(p => p.Action)
                .WithMany(a => a.Permissions)
                .HasForeignKey(p => p.ActionUuid)
                .IsRequired(false)
                .OnDelete(DeleteBehavior.ClientCascade);
        }
    }
}
