using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemNutrientConfiguration : IEntityTypeConfiguration<MealItemNutrient>
{
    public void Configure(EntityTypeBuilder<MealItemNutrient> builder)
    {
        builder.ToTable("meal_item_nutrients");

        builder.HasKey(min => new { min.MealItemId, min.NutrientId });

        builder.Property(min => min.MealItemId)
            .HasColumnName("meal_item_id").HasColumnType("CHAR(36)");
        builder.Property(min => min.NutrientId)
            .HasColumnName("nutrient_id").HasColumnType("CHAR(36)");
        builder.Property(min => min.Amount)
            .IsRequired().HasColumnName("amount").HasColumnType("DECIMAL(10,3)");

        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_nutrient_amount", "amount >= 0");
        });

        builder.HasOne(min => min.MealItem)
            .WithMany(i => i.Nutrients).HasForeignKey(min => min.MealItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(min => min.Nutrient)
            .WithMany(n => n.MealItemNutrients).HasForeignKey(min => min.NutrientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
