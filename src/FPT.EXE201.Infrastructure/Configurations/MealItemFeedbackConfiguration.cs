using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MealItemFeedbackConfiguration : IEntityTypeConfiguration<MealItemFeedback>
{
    public void Configure(EntityTypeBuilder<MealItemFeedback> builder)
    {
        builder.ToTable("meal_item_feedback");

        builder.HasKey(f => f.Id);
        builder.Property(f => f.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(f => f.MealItemId)
            .IsRequired().HasColumnName("meal_item_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.UserId)
            .IsRequired().HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(f => f.Liked)
            .IsRequired().HasColumnName("liked").HasColumnType("TINYINT(1)");
        builder.Property(f => f.Comment)
            .HasColumnName("comment").HasMaxLength(300);

        builder.Property(f => f.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(f => f.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(f => f.IsDeleted);

        builder.HasIndex(f => new { f.MealItemId, f.UserId })
            .IsUnique().HasDatabaseName("uk_meal_item_feedback");

        builder.HasOne(f => f.MealItem)
            .WithMany(i => i.Feedbacks).HasForeignKey(f => f.MealItemId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(f => f.User)
            .WithMany().HasForeignKey(f => f.UserId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
