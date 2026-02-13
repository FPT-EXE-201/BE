using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PrenatalTestConfiguration : IEntityTypeConfiguration<PrenatalTest>
{
    public void Configure(EntityTypeBuilder<PrenatalTest> builder)
    {
        builder.ToTable("prenatal_tests");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.VisitId).HasColumnName("visit_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.TestTypeId).IsRequired().HasColumnName("test_type_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.TestDate).IsRequired().HasColumnName("test_date").HasColumnType("DATE");
        builder.Property(p => p.ImageUrlsJson).HasColumnName("image_urls").HasColumnType("JSON");
        builder.Property(p => p.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(p => p.IsAbnormalResult).IsRequired().HasColumnName("abnormal_flag")
            .HasColumnType("TINYINT(1)").HasDefaultValue(false);
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_prenatal_tests_pregnancy");
        builder.HasIndex(p => p.VisitId).HasDatabaseName("idx_prenatal_tests_visit");
        builder.HasIndex(p => p.TestDate).HasDatabaseName("idx_prenatal_tests_date");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Tests).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Visit)
            .WithMany(v => v.Tests).HasForeignKey(p => p.VisitId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(p => p.TestType)
            .WithMany(tt => tt.Tests).HasForeignKey(p => p.TestTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
