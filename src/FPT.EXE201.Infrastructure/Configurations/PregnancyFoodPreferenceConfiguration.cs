using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyFoodPreferenceConfiguration
    : IEntityTypeConfiguration<PregnancyFoodPreference>
{
    public void Configure(EntityTypeBuilder<PregnancyFoodPreference> builder)
    {
        builder.ToTable("pregnancy_food_preferences");

        builder.HasKey(p => p.Id);
        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(p => p.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.FoodItemId)
            .IsRequired().HasColumnName("food_item_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PreferenceType)
            .IsRequired().HasColumnName("preference_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Severity)
            .HasColumnName("severity")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(p => p.Notes)
            .HasColumnName("notes").HasMaxLength(255);

        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(p => p.IsDeleted);

        // Unique: 1 preference per pregnancy + food + type
        builder.HasIndex(p => new { p.PregnancyId, p.FoodItemId, p.PreferenceType })
            .IsUnique().HasDatabaseName("uk_food_pref_pregnancy");

        // Relationships
        builder.HasOne(p => p.Pregnancy)
            .WithMany(preg => preg.FoodPreferences).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.FoodItem)
            .WithMany(f => f.FoodPreferences).HasForeignKey(p => p.FoodItemId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
