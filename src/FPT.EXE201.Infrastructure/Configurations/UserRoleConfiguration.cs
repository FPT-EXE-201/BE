using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class UserRoleConfiguration : IEntityTypeConfiguration<UserRole>
    {
        public void Configure(EntityTypeBuilder<UserRole> builder)
        {
            // Table name
            builder.ToTable("user_roles");

            // Composite primary key
            builder.HasKey(e => new { e.UserId, e.RoleId });

            // Properties
            builder.Property(e => e.UserId)
                .HasColumnName("user_id");

            builder.Property(e => e.RoleId)
                .HasColumnName("role_id");

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .HasColumnType("datetime(3)")
                .IsRequired()
                .HasDefaultValueSql("CURRENT_TIMESTAMP(3)");

            // Indexes
            builder.HasIndex(e => e.RoleId)
                .HasDatabaseName("idx_user_roles_role");

            // Relationships
            // Foreign key constraints will be defined in User and Role configurations
        }
    }
}
