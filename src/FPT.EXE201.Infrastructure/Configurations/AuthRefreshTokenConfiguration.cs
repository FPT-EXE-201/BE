using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class AuthRefreshTokenConfiguration : IEntityTypeConfiguration<AuthRefreshToken>
    {
        public void Configure(EntityTypeBuilder<AuthRefreshToken> builder)
        {
            // Table name
            builder.ToTable("auth_refresh_tokens");

            // Primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Properties
            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(e => e.Jti)
                .HasColumnName("jti")
                .IsRequired();

            builder.Property(e => e.TokenHash)
                .HasColumnName("token_hash")
                .HasColumnType("binary(32)")
                .IsRequired();

            builder.Property(e => e.IssuedAt)
                .HasColumnName("issued_at")
                .HasColumnType("datetime(3)")
                .IsRequired();

            builder.Property(e => e.ExpiresAt)
                .HasColumnName("expires_at")
                .HasColumnType("datetime(3)")
                .IsRequired();

            builder.Property(e => e.RevokedAt)
                .HasColumnName("revoked_at")
                .HasColumnType("datetime(3)")
                .IsRequired(false);

            builder.Property(e => e.RotatedFromId)
                .HasColumnName("rotated_from_id")
                .IsRequired(false);

            // Device & Security info
            builder.Property(e => e.DeviceInfo)
                .HasColumnName("device_info")
                .HasColumnType("json")
                .IsRequired(false);

            builder.Property(e => e.IpAddress)
                .HasColumnName("ip_address")
                .HasMaxLength(45)
                .IsRequired(false);

            builder.Property(e => e.UserAgent)
                .HasColumnName("user_agent")
                .HasMaxLength(512)
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

            // Indexes
            builder.HasIndex(e => e.Jti)
                .IsUnique()
                .HasDatabaseName("uq_refresh_jti");

            builder.HasIndex(e => e.TokenHash)
                .IsUnique()
                .HasDatabaseName("uq_refresh_token_hash");

            builder.HasIndex(e => new { e.UserId, e.ExpiresAt })
                .HasDatabaseName("idx_refresh_user_expires");

            builder.HasIndex(e => new { e.UserId, e.RevokedAt })
                .HasDatabaseName("idx_refresh_user_revoked");

            // Check constraint
            // Note: EF Core may not create this automatically, you might need to add it via migration
            // builder.HasCheckConstraint("chk_refresh_exp", "expires_at > issued_at");

            // Relationships
            builder.HasOne(e => e.User)
                .WithMany()
                .HasForeignKey(e => e.UserId)
                .HasConstraintName("fk_refresh_user")
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(e => e.RotatedFrom)
                .WithMany()
                .HasForeignKey(e => e.RotatedFromId)
                .HasConstraintName("fk_refresh_rotated_from")
                .OnDelete(DeleteBehavior.SetNull);
        }
    }
}
