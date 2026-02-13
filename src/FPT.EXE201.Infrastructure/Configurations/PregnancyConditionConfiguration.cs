using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class PregnancyConditionConfiguration : IEntityTypeConfiguration<PregnancyCondition>
{
    public void Configure(EntityTypeBuilder<PregnancyCondition> builder)
    {
        builder.ToTable("pregnancy_conditions");

        builder.Property(p => p.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(p => p.PregnancyId).IsRequired().HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.ConditionId).IsRequired().HasColumnName("condition_id").HasColumnType("CHAR(36)");
        builder.Property(p => p.DiagnosedDate).HasColumnName("diagnosed_at").HasColumnType("DATE");
        builder.Property(p => p.Severity).HasColumnName("severity").HasConversion<string?>().HasMaxLength(20);
        builder.Property(p => p.Notes).HasColumnName("notes").HasColumnType("TEXT");
        builder.Property(p => p.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(p => p.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(p => p.IsDeleted);

        builder.HasIndex(p => p.PregnancyId).HasDatabaseName("idx_pregnancy_conditions_pregnancy");
        builder.HasIndex(p => p.ConditionId).HasDatabaseName("idx_pregnancy_conditions_condition");
        builder.HasIndex(p => new { p.PregnancyId, p.ConditionId })
            .IsUnique().HasDatabaseName("uk_pregnancy_condition");

        builder.HasOne(p => p.Pregnancy)
            .WithMany(pr => pr.Conditions).HasForeignKey(p => p.PregnancyId)
            .OnDelete(DeleteBehavior.Cascade);
        builder.HasOne(p => p.Condition)
            .WithMany(c => c.PregnancyConditions).HasForeignKey(p => p.ConditionId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
