using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PrenatalVisitConfiguration : IEntityTypeConfiguration<PrenatalVisit>
{
    public void Configure(EntityTypeBuilder<PrenatalVisit> builder)
    {
        builder.ToTable("prenatal_visits");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.DoctorId).HasColumnName("doctor_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.VisitDate).IsRequired().HasColumnName("visit_date").HasColumnType("DATE");
        builder.Property(p => p.VisitType).IsRequired().HasColumnName("visit_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Location).HasColumnName("location").HasMaxLength(200);
        builder.Property(p => p.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(p => p.VitalsJson).HasColumnName("vitals_json").HasColumnType("JSON");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_prenatal_visits_pregnancy");
        builder.HasIndex(p => p.VisitDate).HasDatabaseName("idx_prenatal_visits_date");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Visits).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasMany(p => p.Tests)
            .WithOne(t => t.Visit).HasForeignKey(t => t.VisitId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
