using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefDocumentTypeConfiguration : IEntityTypeConfiguration<RefDocumentType>
{
    public void Configure(EntityTypeBuilder<RefDocumentType> builder)
    {
        builder.ToTable("ref_document_types");

        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Code).IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code).IsUnique().HasDatabaseName("uk_ref_doc_types_code");
        builder.Property(r => r.IsActive).IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(r => r.IsDeleted);

        // Relationships
        builder.HasMany(r => r.Translations)
            .WithOne(t => t.DocumentType).HasForeignKey(t => t.DocumentTypeId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
