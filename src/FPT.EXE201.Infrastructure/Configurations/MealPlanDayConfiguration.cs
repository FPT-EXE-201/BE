using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanDayConfiguration : IEntityTypeConfiguration<MealPlanDay>
{
    public void Configure(EntityTypeBuilder<MealPlanDay> builder)
    {
        builder.ToTable("meal_plan_days");

        builder.HasKey(d => d.Id);
        builder.Property(d => d.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(d => d.MealPlanId)
            .IsRequired().HasColumnName("meal_plan_id").HasColumnType("CHAR(36)");
        builder.Property(d => d.PlanDate)
            .IsRequired().HasColumnName("plan_date").HasColumnType("DATE");

        builder.Property(d => d.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(d => d.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(d => d.IsDeleted);

        builder.HasIndex(d => new { d.MealPlanId, d.PlanDate })
            .IsUnique().HasDatabaseName("uk_meal_plan_days");

        builder.HasOne(d => d.MealPlan)
            .WithMany(m => m.Days).HasForeignKey(d => d.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
