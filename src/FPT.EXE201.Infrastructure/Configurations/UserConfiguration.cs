using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class UserConfiguration : IEntityTypeConfiguration<User>
    {
        public void Configure(EntityTypeBuilder<User> builder)
        {
            // Table name - snake_case
            builder.ToTable("users");

            // Primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Properties - snake_case columns
            builder.Property(e => e.Email)
                .HasColumnName("email")
                .HasMaxLength(255)
                .UseCollation("utf8mb4_unicode_ci");

            builder.Property(e => e.Phone)
                .HasColumnName("phone")
                .HasMaxLength(20)
                .UseCollation("utf8mb4_unicode_ci");

            builder.Property(e => e.PasswordHash)
                .HasColumnName("password_hash")
                .HasColumnType("varbinary(255)")
                .IsRequired();

            builder.Property(e => e.Status)
                .HasColumnName("status")
                .IsRequired()
                .HasMaxLength(50)
                .HasConversion<string>() // Store enum as string
                .UseCollation("utf8mb4_unicode_ci");

            builder.Property(e => e.IsEmailVerified)
                .HasColumnName("is_email_verified")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.IsPhoneVerified)
                .HasColumnName("is_phone_verified")
                .IsRequired()
                .HasDefaultValue(false);

            builder.Property(e => e.LastLoginAt)
                .HasColumnName("last_login_at")
                .IsRequired(false);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            builder.Property(e => e.DeletedAt)
                .HasColumnName("deleted_at")
                .IsRequired(false);

            // Ignore computed property
            builder.Ignore(e => e.IsDeleted);

        
            // Indexes
            builder.HasIndex(e => e.Email)
                .IsUnique();

            builder.HasIndex(e => e.Phone)
                .IsUnique();

        
            builder.HasIndex(e => e.DeletedAt);

            // Relationships
            // User -> UserProfile (One-to-One)
            builder.HasOne(e => e.Profile)
                .WithOne(p => p.User)
                .HasForeignKey<UserProfile>(p => p.UserId)
                .OnDelete(DeleteBehavior.Cascade);

            // User -> UserRoles (One-to-Many)
            builder.HasMany(e => e.UserRoles)
                .WithOne(ur => ur.User)
                .HasForeignKey(ur => ur.UserId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
