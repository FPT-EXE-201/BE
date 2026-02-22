using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MotivationalTemplateTranslationConfiguration
    : IEntityTypeConfiguration<MotivationalTemplateTranslation>
{
    public void Configure(EntityTypeBuilder<MotivationalTemplateTranslation> builder)
    {
        builder.ToTable("motivational_template_translations");

        // Composite primary key
        builder.HasKey(tr => new { tr.TemplateId, tr.LanguageCode });

        builder.Property(tr => tr.TemplateId)
            .HasColumnName("template_id").HasColumnType("CHAR(36)");

        builder.Property(tr => tr.LanguageCode)
            .HasColumnName("language_code").HasMaxLength(10)
            .UseCollation("utf8mb4_unicode_ci");

        builder.Property(tr => tr.Title)
            .HasColumnName("title").HasMaxLength(120);

        builder.Property(tr => tr.Message)
            .IsRequired().HasColumnName("message").HasMaxLength(500);

        // Relationships
        builder.HasOne(tr => tr.Template)
            .WithMany(t => t.Translations)
            .HasForeignKey(tr => tr.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(tr => tr.Language)
            .WithMany().HasForeignKey(tr => tr.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
