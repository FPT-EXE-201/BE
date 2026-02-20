using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefDocumentTypeTranslationConfiguration
    : IEntityTypeConfiguration<RefDocumentTypeTranslation>
{
    public void Configure(EntityTypeBuilder<RefDocumentTypeTranslation> builder)
    {
        builder.ToTable("ref_document_type_translations");

        builder.HasKey(t => new { t.DocumentTypeId, t.LanguageCode });

        builder.Property(t => t.DocumentTypeId).HasColumnName("document_type_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode).IsRequired().HasColumnName("language_code").HasMaxLength(10).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName).IsRequired().HasColumnName("display_name").HasMaxLength(200);
        builder.Property(t => t.Description).HasColumnName("description").HasColumnType("TEXT");

        // Relationships
        builder.HasOne(t => t.DocumentType)
            .WithMany(d => d.Translations).HasForeignKey(t => t.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
