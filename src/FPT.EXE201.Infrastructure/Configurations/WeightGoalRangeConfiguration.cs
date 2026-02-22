using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class WeightGoalRangeConfiguration : IEntityTypeConfiguration<WeightGoalRange>
{
    public void Configure(EntityTypeBuilder<WeightGoalRange> builder)
    {
        builder.ToTable("weight_goal_ranges");

        builder.HasKey(g => g.Id);
        builder.Property(g => g.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(g => g.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");

        builder.Property(g => g.HeightCm)
            .HasColumnName("height_cm").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.PrePregnancyWeightKg)
            .HasColumnName("pre_pregnancy_weight_kg").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.Bmi)
            .HasColumnName("bmi").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.RecommendedTotalGainMin)
            .HasColumnName("recommended_total_gain_min").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.RecommendedTotalGainMax)
            .HasColumnName("recommended_total_gain_max").HasColumnType("DECIMAL(5,2)");

        builder.Property(g => g.Notes)
            .HasColumnName("notes").HasMaxLength(500);

        builder.Property(g => g.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(g => g.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(g => g.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(g => g.IsDeleted);

        // Unique: 1 goal per pregnancy
        builder.HasIndex(g => g.PregnancyId)
            .IsUnique().HasDatabaseName("uk_weight_goals_pregnancy");

        // Relationships
        builder.HasOne(g => g.Pregnancy)
            .WithMany().HasForeignKey(g => g.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
