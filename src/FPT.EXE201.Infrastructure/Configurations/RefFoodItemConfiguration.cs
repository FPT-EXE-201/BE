using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RefFoodItemConfiguration : IEntityTypeConfiguration<RefFoodItem>
{
    public void Configure(EntityTypeBuilder<RefFoodItem> builder)
    {
        builder.ToTable("ref_food_items");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.Code)
            .IsRequired().HasColumnName("code").HasMaxLength(80);
        builder.HasIndex(r => r.Code)
            .IsUnique().HasDatabaseName("uk_ref_food_items_code");

        builder.Property(r => r.IsActive)
            .IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(r => r.IsDeleted);

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.FoodItem).HasForeignKey(t => t.FoodItemId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
