using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class MotivationalTemplateConfiguration : IEntityTypeConfiguration<MotivationalTemplate>
{
    public void Configure(EntityTypeBuilder<MotivationalTemplate> builder)
    {
        builder.ToTable("motivational_templates");

        builder.HasKey(t => t.Id);
        builder.Property(t => t.Id)
            .HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(t => t.Category)
            .IsRequired().HasColumnName("category")
            .HasConversion<string>().HasMaxLength(30);

        builder.Property(t => t.WeekStart)
            .IsRequired().HasColumnName("week_start");

        builder.Property(t => t.WeekEnd)
            .IsRequired().HasColumnName("week_end");

        builder.Property(t => t.IsActive)
            .IsRequired().HasColumnName("is_active")
            .HasDefaultValue(true);

        builder.Property(t => t.VariablesJson)
            .HasColumnName("variables_json").HasColumnType("JSON");

        builder.Property(t => t.CreatedAt)
            .HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(t => t.UpdatedAt)
            .HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(t => t.DeletedAt)
            .HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(t => t.IsDeleted);

        // Check constraint (matches SQL schema: chk_motivational_week)
        builder.ToTable(t =>
        {
            t.HasCheckConstraint("chk_motivational_week",
                "week_start >= 0 AND week_end >= week_start AND week_end <= 45");
        });

        // Index (composite — includes IsActive per guide spec)
        builder.HasIndex(t => new { t.WeekStart, t.WeekEnd, t.IsActive })
            .HasDatabaseName("idx_motivational_week");

        // Navigation
        builder.HasMany(t => t.Translations)
            .WithOne(tr => tr.Template)
            .HasForeignKey(tr => tr.TemplateId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
