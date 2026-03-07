using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

/// <summary>
/// ⚠️ Decision #2: RefNutrient does NOT inherit BaseEntity.
/// No soft-delete filter, no DeletedAt column.
/// </summary>
public class RefNutrientConfiguration : IEntityTypeConfiguration<RefNutrient>
{
    public void Configure(EntityTypeBuilder<RefNutrient> builder)
    {
        builder.ToTable("ref_nutrients");

        builder.HasKey(r => r.Id);
        builder.Property(r => r.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(r => r.Code)
            .IsRequired().HasColumnName("code").HasMaxLength(50);
        builder.HasIndex(r => r.Code)
            .IsUnique().HasDatabaseName("uk_ref_nutrients_code");

        builder.Property(r => r.Unit)
            .IsRequired().HasColumnName("unit").HasMaxLength(20);

        builder.Property(r => r.IsActive)
            .IsRequired().HasColumnName("is_active")
            .HasColumnType("TINYINT(1)").HasDefaultValue(true);

        builder.Property(r => r.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(r => r.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");

        // ⚠️ NO DeletedAt, NO Ignore(IsDeleted) — custom entity

        builder.HasMany(r => r.Translations)
            .WithOne(t => t.Nutrient).HasForeignKey(t => t.NutrientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
