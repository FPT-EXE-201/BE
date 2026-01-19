using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class PermissionConfiguration : IEntityTypeConfiguration<Permission>
    {
        public void Configure(EntityTypeBuilder<Permission> builder)
        {
            // Table name
            builder.ToTable("permissions");

            // Primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Properties
            builder.Property(e => e.Code)
                .HasColumnName("code")
                .HasMaxLength(80)
                .IsRequired();

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .HasMaxLength(120)
                .IsRequired();

            builder.Property(e => e.Description)
                .HasColumnName("description")
                .HasMaxLength(255)
                .IsRequired(false);

            // Timestamps
            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(3)")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .HasColumnType("datetime(3)")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .HasColumnType("datetime(3)")
                .IsRequired(false);

            // Ignore computed property
            builder.Ignore(e => e.IsDeleted);

            // Indexes
            builder.HasIndex(e => e.Code)
                .IsUnique()
                .HasDatabaseName("uq_permissions_code");

            builder.HasIndex(e => e.DeletedAt)
                .HasDatabaseName("idx_permissions_deleted_at");

            // Relationships
            builder.HasMany(e => e.RolePermissions)
                .WithOne(rp => rp.Permission)
                .HasForeignKey(rp => rp.PermissionId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
