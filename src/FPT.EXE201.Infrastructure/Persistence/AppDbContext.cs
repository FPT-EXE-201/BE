using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence.Seeders;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Persistence
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<UserProfile> UserProfiles { get; set; }
        public DbSet<Language> Languages { get; set; }
        public DbSet<Role> Roles { get; set; }
        public DbSet<Permission> Permissions { get; set; }
        public DbSet<RolePermission> RolePermissions { get; set; }
        public DbSet<UserRole> UserRoles { get; set; }
        public DbSet<AuthRefreshToken> AuthRefreshTokens { get; set; }
        public DbSet<AuditEvent> AuditEvents { get; set; }

        // Week 3 — Pregnancy Core
        public DbSet<Pregnancy> Pregnancies { get; set; }
        public DbSet<RefPregnancyCondition> RefPregnancyConditions { get; set; }
        public DbSet<RefPregnancyConditionTranslation> RefPregnancyConditionTranslations { get; set; }
        public DbSet<PregnancyCondition> PregnancyConditions { get; set; }
        public DbSet<PrenatalVisit> PrenatalVisits { get; set; }
        public DbSet<RefTestType> RefTestTypes { get; set; }
        public DbSet<RefTestTypeTranslation> RefTestTypeTranslations { get; set; }
        public DbSet<PrenatalTest> PrenatalTests { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configure charset for Vietnamese support (utf8mb4 for full Unicode support)
            modelBuilder.HasCharSet("utf8mb4");

            // Apply all entity configurations from current assembly
            modelBuilder.ApplyConfigurationsFromAssembly(typeof(AppDbContext).Assembly);

            // Week 3 Seeders
            PregnancyConditionSeeder.Seed(modelBuilder);
            TestTypeSeeder.Seed(modelBuilder);

            // Apply soft delete query filter for all entities inheriting from BaseEntity
            foreach (var entityType in modelBuilder.Model.GetEntityTypes())
            {
                if (typeof(BaseEntity).IsAssignableFrom(entityType.ClrType))
                {
                    var parameter = System.Linq.Expressions.Expression.Parameter(entityType.ClrType, "e");
                    var property = System.Linq.Expressions.Expression.Property(parameter, nameof(BaseEntity.DeletedAt));
                    var nullConstant = System.Linq.Expressions.Expression.Constant(null, typeof(DateTime?));
                    var equality = System.Linq.Expressions.Expression.Equal(property, nullConstant);
                    var lambda = System.Linq.Expressions.Expression.Lambda(equality, parameter);

                    modelBuilder.Entity(entityType.ClrType).HasQueryFilter(lambda);
                }
            }
        }

        // Override SaveChanges to automatically update timestamps
        public override int SaveChanges()
        {
            UpdateTimestamps();
            return base.SaveChanges();
        }

        public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            UpdateTimestamps();
            return await base.SaveChangesAsync(cancellationToken);
        }

        private void UpdateTimestamps()
        {
            var entries = ChangeTracker.Entries()
                .Where(e => e.State == EntityState.Added || e.State == EntityState.Modified);

            foreach (var entry in entries)
            {
                // Handle BaseEntity
                if (entry.Entity is BaseEntity baseEntity)
                {
                    if (entry.State == EntityState.Added)
                    {
                        baseEntity.CreatedAt = DateTime.UtcNow;
                    }
                    baseEntity.UpdatedAt = DateTime.UtcNow;
                }
                // Handle AuthRefreshToken
                else if (entry.Entity is AuthRefreshToken refreshToken)
                {
                    if (entry.State == EntityState.Added)
                    {
                        refreshToken.CreatedAt = DateTime.UtcNow;
                    }
                    refreshToken.UpdatedAt = DateTime.UtcNow;
                }
                // Handle AuditEvent (only CreatedAt)
                else if (entry.Entity is AuditEvent auditEvent && entry.State == EntityState.Added)
                {
                    auditEvent.CreatedAt = DateTime.UtcNow;
                }
                // Handle RolePermission (only CreatedAt)
                else if (entry.Entity is RolePermission rolePermission && entry.State == EntityState.Added)
                {
                    rolePermission.CreatedAt = DateTime.UtcNow;
                }
                // Handle UserRole (only CreatedAt)
                else if (entry.Entity is UserRole userRole && entry.State == EntityState.Added)
                {
                    userRole.CreatedAt = DateTime.UtcNow;
                }
            }
        }
    }
}
