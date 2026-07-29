using Auth.Common.Models.Entities;
using Auth.Models.Entities;
using Auth.Models.Entities.Configurations;
using Microsoft.EntityFrameworkCore;

namespace Auth.Data
{
    public class AuthDbContext : DbContext
    {
        public AuthDbContext(DbContextOptions options)
        : base(options)
        {
        }

        public DbSet<User> Users { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<Resource> Resources { get; set; }
        public DbSet<Models.Entities.Action> Actions { get; set; }

        public override int SaveChanges()
        {
            UpdateTimestampedEntities();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestampedEntities();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestampedEntities()
        {
            var entities = ChangeTracker.Entries().Where(x => x.Entity is TimestampedEntity && (x.State == EntityState.Added || x.State == EntityState.Modified));

            foreach (var entity in entities)
            {
                var timestampedEntity = (TimestampedEntity) entity.Entity;
                if (entity.State == EntityState.Added) timestampedEntity.CreatedAt = DateTime.UtcNow;
                timestampedEntity.UpdatedAt = DateTime.UtcNow;
            }
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.HasDefaultSchema("auth");
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(UserConfiguration).Assembly);
        }
    }
}
