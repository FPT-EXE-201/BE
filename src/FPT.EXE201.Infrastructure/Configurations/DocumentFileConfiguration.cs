using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class DocumentFileConfiguration : IEntityTypeConfiguration<DocumentFile>
{
    public void Configure(EntityTypeBuilder<DocumentFile> builder)
    {
        builder.ToTable("document_files");

        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(d => d.DocumentId).IsRequired().HasColumnName("document_id").HasColumnType("CHAR(36)");
        builder.Property(d => d.StorageFileId).IsRequired().HasColumnName("storage_file_id").HasColumnType("CHAR(36)");
        builder.Property(d => d.SortOrder).IsRequired().HasColumnName("sort_order").HasDefaultValue(1);
        builder.Property(d => d.PageLabel).HasColumnName("page_label").HasMaxLength(100);

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(d => d.IsDeleted);

        // Indexes
        builder.HasIndex(d => new { d.DocumentId, d.SortOrder })
            .IsUnique().HasDatabaseName("uk_document_files_sort");
        builder.HasIndex(d => d.StorageFileId).HasDatabaseName("idx_document_files_storage");

        // Relationships
        builder.HasOne(d => d.Document)
            .WithMany(m => m.Files)
            .HasForeignKey(d => d.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(d => d.StorageFile)
            .WithMany()
            .HasForeignKey(d => d.StorageFileId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
