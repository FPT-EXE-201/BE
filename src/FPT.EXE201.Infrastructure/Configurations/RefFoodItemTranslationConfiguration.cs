using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefFoodItemTranslationConfiguration
    : IEntityTypeConfiguration<RefFoodItemTranslation>
{
    public void Configure(EntityTypeBuilder<RefFoodItemTranslation> builder)
    {
        builder.ToTable("ref_food_item_translations");

        builder.HasKey(t => new { t.FoodItemId, t.LanguageCode });

        builder.Property(t => t.FoodItemId)
            .HasColumnName("food_item_id").HasColumnType("CHAR(36)");
        builder.Property(t => t.LanguageCode)
            .IsRequired().HasColumnName("language_code")
            .HasMaxLength(10).UseCollation("utf8mb4_unicode_ci");
        builder.Property(t => t.DisplayName)
            .IsRequired().HasColumnName("display_name").HasMaxLength(120);

        builder.HasOne(t => t.FoodItem)
            .WithMany(f => f.Translations).HasForeignKey(t => t.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(t => t.Language)
            .WithMany().HasForeignKey(t => t.LanguageCode)
            .HasPrincipalKey(l => l.Code)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
