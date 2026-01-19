using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class RolePermissionConfiguration : IEntityTypeConfiguration<RolePermission>
    {
        public void Configure(EntityTypeBuilder<RolePermission> builder)
        {
            // Table name
            builder.ToTable("role_permissions");

            // Composite primary key
            builder.HasKey(e => new { e.RoleId, e.PermissionId });

            // Properties
            builder.Property(e => e.RoleId)
                .HasColumnName("role_id");

            builder.Property(e => e.PermissionId)
                .HasColumnName("permission_id");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(3)")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            // Relationships (configured in Role and Permission entities)
            // Foreign key constraints: fk_role_permissions_role, fk_role_permissions_perm
        }
    }
}
