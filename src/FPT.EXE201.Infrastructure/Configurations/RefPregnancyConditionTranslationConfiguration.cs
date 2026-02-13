using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefPregnancyConditionTranslationConfiguration
    : IEntityTypeConfiguration<RefPregnancyConditionTranslation>
{
    public void Configure(EntityTypeBuilder<RefPregnancyConditionTranslation> builder)
    {
        builder.ToTable("ref_pregnancy_condition_translations");

        builder.HasKey(t => new { t.ConditionId, t.LanguageCode });

        builder.Property(t => t.ConditionId).HasColumnName("condition_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("lang_code").HasMaxLength(5).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        builder.HasOne(t => t.Condition)
            .WithMany(c => c.Translations).HasForeignKey(t => t.ConditionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
