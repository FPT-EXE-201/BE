using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightAlertConfiguration : IEntityTypeConfiguration<WeightAlert>
{
    public void Configure(EntityTypeBuilder<WeightAlert> builder)
    {
        builder.ToTable("weight_alerts");

        builder.HasKey(a => a.Id);

        builder.Property(a => a.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(a => a.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(a => a.AlertType)
            .IsRequired().HasColumnName("alert_type")
            .HasConversion<string>().HasMaxLength(64);

        builder.Property(a => a.TriggeredAt)
            .IsRequired().HasColumnName("triggered_at").HasColumnType("DATETIME(6)");

        builder.Property(a => a.DetailsJson)
            .HasColumnName("details_json").HasColumnType("JSON");

        builder.Property(a => a.ResolvedAt)
            .HasColumnName("resolved_at").HasColumnType("DATETIME(6)");

        // Indexes (composite — per WEEK_6_PROMPTS_GUIDE spec)
        builder.HasIndex(a => new { a.PregnancyId, a.TriggeredAt })
            .HasDatabaseName("idx_weight_alerts_pregnancy");

        builder.HasIndex(a => new { a.AlertType, a.TriggeredAt })
            .HasDatabaseName("idx_weight_alerts_type");

        // Relationships
        builder.HasOne(a => a.Pregnancy)
            .WithMany().HasForeignKey(a => a.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
