using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemConfiguration : IEntityTypeConfiguration<MealItem>
{
    public void Configure(EntityTypeBuilder<MealItem> builder)
    {
        builder.ToTable("meal_items");

        builder.HasKey(i => i.Id);
        builder.Property(i => i.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(i => i.MealDayId)
            .IsRequired().HasColumnName("meal_day_id").HasColumnType("CHAR(36)");
        builder.Property(i => i.MealType)
            .IsRequired().HasColumnName("meal_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(i => i.RecipeId)
            .HasColumnName("recipe_id").HasColumnType("CHAR(36)");
        builder.Property(i => i.ItemName)
            .HasColumnName("item_name").HasMaxLength(200);
        builder.Property(i => i.PortionText)
            .HasColumnName("portion_text").HasMaxLength(120);
        builder.Property(i => i.CaloriesKcal)
            .HasColumnName("calories_kcal");
        builder.Property(i => i.Notes)
            .HasColumnName("notes").HasMaxLength(255);

        builder.Property(i => i.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(i => i.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(i => i.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(i => i.IsDeleted);

        builder.HasIndex(i => new { i.MealDayId, i.MealType })
            .HasDatabaseName("idx_meal_items_day_type");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_meal_item_name",
                "recipe_id IS NOT NULL OR item_name IS NOT NULL");
        });

        // Relationships
        builder.HasOne(i => i.MealDay)
            .WithMany(d => d.Items).HasForeignKey(i => i.MealDayId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(i => i.Recipe)
            .WithMany(r => r.MealItems).HasForeignKey(i => i.RecipeId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
