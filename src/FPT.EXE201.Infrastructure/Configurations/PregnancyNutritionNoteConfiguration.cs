using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyNutritionNoteConfiguration
    : IEntityTypeConfiguration<PregnancyNutritionNote>
{
    public void Configure(EntityTypeBuilder<PregnancyNutritionNote> builder)
    {
        builder.ToTable("pregnancy_nutrition_notes");

        builder.HasKey(n => n.Id);
        builder.Property(n => n.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(n => n.PregnancyId)
            .IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(n => n.NoteType)
            .IsRequired().HasColumnName("note_type")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(n => n.ValueText)
            .IsRequired().HasColumnName("value_text").HasMaxLength(200);

        builder.Property(n => n.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(n => n.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(n => n.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(n => n.IsDeleted);

        builder.HasIndex(n => new { n.PregnancyId, n.CreatedAt })
            .HasDatabaseName("idx_nutrition_notes_pregnancy");

        builder.HasOne(n => n.Pregnancy)
            .WithMany(preg => preg.NutritionNotes).HasForeignKey(n => n.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
