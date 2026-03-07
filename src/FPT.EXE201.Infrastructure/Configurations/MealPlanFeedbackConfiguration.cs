using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealPlanFeedbackConfiguration : IEntityTypeConfiguration<MealPlanFeedback>
{
    public void Configure(EntityTypeBuilder<MealPlanFeedback> builder)
    {
        builder.ToTable("meal_plan_feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(f => f.MealPlanId)
            .IsRequired().HasColumnName("meal_plan_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.Rating)
            .IsRequired().HasColumnName("rating").HasColumnType("TINYINT");
        builder.Property(f => f.Comment)
            .HasColumnName("comment").HasMaxLength(500);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(f => f.IsDeleted);

        builder.HasIndex(f => new { f.MealPlanId, f.UserId })
            .IsUnique().HasDatabaseName("uk_meal_plan_feedback");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_plan_rating", "rating BETWEEN 1 AND 5");
        });

        builder.HasOne(f => f.MealPlan)
            .WithMany(m => m.Feedbacks).HasForeignKey(f => f.MealPlanId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.User)
            .WithMany().HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
