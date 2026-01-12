using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class UserProfileConfiguration : IEntityTypeConfiguration<UserProfile>
    {
        public void Configure(EntityTypeBuilder<UserProfile> builder)
        {
            // Table name - snake_case
            builder.ToTable("user_profiles");

            // Primary key
            builder.HasKey(e => e.Id);
            builder.Property(e => e.Id)
                .HasColumnName("id");

            // Properties - snake_case columns
            builder.Property(e => e.UserId)
                .HasColumnName("user_id")
                .IsRequired();

            builder.Property(e => e.FullName)
                .HasColumnName("full_name")
                .HasMaxLength(200);

            builder.Property(e => e.DateOfBirth)
                .HasColumnName("date_of_birth")
                .IsRequired(false);

            builder.Property(e => e.AvatarUrl)
                .HasColumnName("avatar_url")
                .HasMaxLength(500);

            builder.Property(e => e.PreferredLang)
                .HasColumnName("preferred_lang")
                .IsRequired()
                .HasMaxLength(10)
                .HasDefaultValue("vi");

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
            builder.HasIndex(e => e.UserId)
                .IsUnique();

            builder.HasIndex(e => e.DeletedAt);

            // Relationships
            // UserProfile -> Language (Many-to-One)
            builder.HasOne(e => e.PreferredLanguage)
                .WithMany()
                .HasForeignKey(e => e.PreferredLang)
                .HasPrincipalKey(l => l.Code)
                .OnDelete(DeleteBehavior.Restrict);
        }
    }
}
