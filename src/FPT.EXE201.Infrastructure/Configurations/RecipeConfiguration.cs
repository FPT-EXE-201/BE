using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("recipes");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(r => r.Title)
            .IsRequired().HasColumnName("title").HasMaxLength(200);
        builder.Property(r => r.Instructions)
            .HasColumnName("instructions").HasColumnType("LONGTEXT");
        builder.Property(r => r.Servings)
            .HasColumnName("servings");
        builder.Property(r => r.PrepMinutes)
            .HasColumnName("prep_minutes");
        builder.Property(r => r.CookMinutes)
            .HasColumnName("cook_minutes");

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(r => r.IsDeleted);

        builder.HasIndex(r => new { r.PregnancyId, r.CreatedAt })
            .HasDatabaseName("idx_recipes_pregnancy");

        builder.HasOne(r => r.Pregnancy)
            .WithMany(preg => preg.Recipes).HasForeignKey(r => r.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
