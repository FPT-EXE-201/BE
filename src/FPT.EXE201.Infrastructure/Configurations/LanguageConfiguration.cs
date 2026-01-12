using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations
{
    public class LanguageConfiguration : IEntityTypeConfiguration<Language>
    {
        public void Configure(EntityTypeBuilder<Language> builder)
        {
            // Table name - snake_case
            builder.ToTable("languages");

            // Primary key
            builder.HasKey(e => e.Code);

            // Properties - snake_case columns
            builder.Property(e => e.Code)
                .HasColumnName("code")
                .IsRequired()
                .HasMaxLength(10);

            builder.Property(e => e.Name)
                .HasColumnName("name")
                .IsRequired()
                .HasMaxLength(100);

            builder.Property(e => e.IsActive)
                .HasColumnName("is_active")
                .IsRequired()
                .HasDefaultValue(true);

            builder.Property(e => e.CreatedAt)
                .HasColumnName("created_at")
                .IsRequired();

            builder.Property(e => e.UpdatedAt)
                .HasColumnName("updated_at")
                .IsRequired();

            // Indexes
            builder.HasIndex(e => e.IsActive);

            // Seed data
            builder.HasData(
                new Language
                {
                    Code = "vi",
                    Name = "Tiếng Việt",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                },
                new Language
                {
                    Code = "en",
                    Name = "English",
                    IsActive = true,
                    CreatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc),
                    UpdatedAt = new DateTime(2026, 1, 1, 0, 0, 0, DateTimeKind.Utc)
                }
            );
        }
    }
}
