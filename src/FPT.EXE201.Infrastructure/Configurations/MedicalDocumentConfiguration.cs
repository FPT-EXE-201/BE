using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MedicalDocumentConfiguration : IEntityTypeConfiguration<MedicalDocument>
{
    public void Configure(EntityTypeBuilder<MedicalDocument> builder)
    {
        builder.ToTable("medical_documents");

        builder.Property(m => m.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(m => m.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.VisitId).HasColumnName("visit_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.DocumentTypeId).HasColumnName("document_type_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.Title).HasColumnName("title").HasMaxLength(200);
        builder.Property(m => m.DocumentDate).HasColumnName("document_date").HasColumnType("DATE");
        builder.Property(m => m.CapturedAt).IsRequired().HasColumnName("captured_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.Source).IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(m => m.IsFavorite).IsRequired().HasColumnName("is_favorite")
            .HasColumnType("TINYINT(1)").HasDefaultValue(false);

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(m => m.IsDeleted);

        // Indexes
        builder.HasIndex(m => new { m.PregnancyId, m.CapturedAt }).HasDatabaseName("idx_medical_docs_pregnancy");
        builder.HasIndex(m => m.VisitId).HasDatabaseName("idx_medical_docs_visit");
        builder.HasIndex(m => new { m.PregnancyId, m.DocumentTypeId, m.CapturedAt }).HasDatabaseName("idx_medical_docs_type");

        // Relationships
        builder.HasOne(m => m.Pregnancy)
            .WithMany().HasForeignKey(m => m.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(m => m.Visit)
            .WithMany(v => v.Documents).HasForeignKey(m => m.VisitId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasOne(m => m.DocumentType)
            .WithMany(d => d.Documents).HasForeignKey(m => m.DocumentTypeId)
            .OnDelete(DeleteBehavior.SetNull);

        builder.HasMany(m => m.Files)
            .WithOne(f => f.Document).HasForeignKey(f => f.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(m => m.OcrResults)
            .WithOne(o => o.Document).HasForeignKey(o => o.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
