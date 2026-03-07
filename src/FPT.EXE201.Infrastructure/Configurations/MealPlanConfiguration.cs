using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanConfiguration : IEntityTypeConfiguration<MealPlan>
{
    public void Configure(EntityTypeBuilder<MealPlan> builder)
    {
        builder.ToTable("meal_plans");

        builder.HasKey(m => m.Id);
        builder.Property(m => m.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(m => m.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.AiRequestLogId)
            .HasColumnName("ai_request_log_id").HasColumnType("CHAR(36)");
        builder.Property(m => m.StartDate)
            .IsRequired().HasColumnName("start_date").HasColumnType("DATE");
        builder.Property(m => m.EndDate)
            .IsRequired().HasColumnName("end_date").HasColumnType("DATE");
        builder.Property(m => m.Source)
            .IsRequired().HasColumnName("source")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(m => m.Title)
            .HasColumnName("title").HasMaxLength(200);
        builder.Property(m => m.Notes)
            .HasColumnName("notes").HasColumnType("TEXT");

        builder.Property(m => m.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(m => m.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(m => m.IsDeleted);

        builder.HasIndex(m => new { m.PregnancyId, m.StartDate })
            .HasDatabaseName("idx_meal_plans_pregnancy");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_meal_plan_dates", "end_date >= start_date");
        });

        // Relationships
        builder.HasOne(m => m.Pregnancy)
            .WithMany(preg => preg.MealPlans).HasForeignKey(m => m.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(m => m.AiRequestLog)
            .WithMany().HasForeignKey(m => m.AiRequestLogId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
