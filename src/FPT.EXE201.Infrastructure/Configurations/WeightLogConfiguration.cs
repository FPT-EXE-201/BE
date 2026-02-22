using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightLogConfiguration : IEntityTypeConfiguration<WeightLog>
{
    public void Configure(EntityTypeBuilder<WeightLog> builder)
    {
        builder.ToTable("weight_logs");

        builder.HasKey(w => w.Id);
        builder.Property(w => w.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(w => w.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(w => w.LoggedOn)
            .IsRequired().HasColumnName("logged_on").HasColumnType("DATE");

        builder.Property(w => w.WeightKg)
            .IsRequired().HasColumnName("weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(w => w.Note)
            .HasColumnName("note").HasMaxLength(255);

        builder.Property(w => w.Source)
            .IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);

        builder.Property(w => w.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(w => w.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(w => w.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(w => w.IsDeleted);

        // Unique: 1 weight log per day per pregnancy
        builder.HasIndex(w => new { w.PregnancyId, w.LoggedOn })
            .IsUnique().HasDatabaseName("uk_weight_logs_pregnancy_date");

        // Relationships
        builder.HasOne(w => w.Pregnancy)
            .WithMany().HasForeignKey(w => w.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
