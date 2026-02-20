using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class StorageFileConfiguration : IEntityTypeConfiguration<StorageFile>
{
    public void Configure(EntityTypeBuilder<StorageFile> builder)
    {
        builder.ToTable("storage_files");

        builder.Property(s => s.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(s => s.OwnerUserId).HasColumnName("owner_user_id").HasColumnType("CHAR(36)");
        builder.Property(s => s.StorageProvider).IsRequired().HasColumnName("storage_provider").HasMaxLength(32)
            .HasDefaultValue("stub");
        builder.Property(s => s.BucketName).HasColumnName("bucket_name").HasMaxLength(128);
        builder.Property(s => s.ObjectKey).IsRequired().HasColumnName("object_key").HasMaxLength(500);
        builder.Property(s => s.PublicUrl).HasColumnName("public_url").HasMaxLength(1000);
        builder.Property(s => s.OriginalFileName).HasColumnName("original_file_name").HasMaxLength(255);
        builder.Property(s => s.MimeType).IsRequired().HasColumnName("mime_type").HasMaxLength(100);
        builder.Property(s => s.FileSizeBytes).IsRequired().HasColumnName("file_size_bytes");
        builder.Property(s => s.ChecksumSha256).HasColumnName("checksum_sha256").HasColumnType("BINARY(32)");
        builder.Property(s => s.UploadedAt).IsRequired().HasColumnName("uploaded_at").HasColumnType("DATETIME(6)");

        builder.Property(s => s.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(s => s.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(s => s.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(s => s.IsDeleted);

        // Indexes
        builder.HasIndex(s => s.OwnerUserId).HasDatabaseName("idx_storage_files_owner");
        builder.HasIndex(new[] { "StorageProvider", "ObjectKey" }).HasDatabaseName("idx_storage_files_object");

        // Relationships
        builder.HasOne(s => s.Owner)
            .WithMany().HasForeignKey(s => s.OwnerUserId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
