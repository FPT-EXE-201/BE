using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefNutrientTranslationConfiguration
    : IEntityTypeConfiguration<RefNutrientTranslation>
{
    public void Configure(EntityTypeBuilder<RefNutrientTranslation> builder)
    {
        builder.ToTable("ref_nutrient_translations");

        builder.HasKey(t => new { t.NutrientId, t.LanguageCode });

        builder.Property(t => t.NutrientId)
            .HasColumnName("nutrient_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode)
            .IsRequired().HasColumnName("language_code")
            .HasMaxLength(10).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName)
            .IsRequired().HasColumnName("display_name").HasMaxLength(120);

        builder.HasOne(t => t.Nutrient)
            .WithMany(n => n.Translations).HasForeignKey(t => t.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
