using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class AiRequestLogConfiguration : IEntityTypeConfiguration<AiRequestLog>
{
    public void Configure(EntityTypeBuilder<AiRequestLog> builder)
    {
        builder.ToTable("ai_request_logs");

        builder.HasKey(a => a.Id);
        builder.Property(a => a.Id).HasColumnName("id").HasColumnType("CHAR(36)");

        builder.Property(a => a.Feature)
            .IsRequired().HasColumnName("feature")
            .HasConversion<string>().HasMaxLength(50);
        builder.Property(a => a.PregnancyId)
            .HasColumnName("pregnancy_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.UserId)
            .HasColumnName("user_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.TemplateId)
            .HasColumnName("template_id").HasColumnType("CHAR(36)");
        builder.Property(a => a.Status)
            .IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(a => a.Model)
            .HasColumnName("model").HasMaxLength(80);
        builder.Property(a => a.PromptVersion)
            .HasColumnName("prompt_version").HasMaxLength(64);
        builder.Property(a => a.RequestPayload)
            .HasColumnName("request_payload").HasColumnType("JSON");
        builder.Property(a => a.ResponsePayload)
            .HasColumnName("response_payload").HasColumnType("LONGTEXT");
        builder.Property(a => a.TokensInput)
            .HasColumnName("tokens_input");
        builder.Property(a => a.TokensOutput)
            .HasColumnName("tokens_output");
        builder.Property(a => a.ProcessingTimeMs)
            .HasColumnName("processing_time_ms");
        builder.Property(a => a.ErrorMessage)
            .HasColumnName("error_message").HasMaxLength(500);

        builder.Property(a => a.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(a => a.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(a => a.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");
        builder.Ignore(a => a.IsDeleted);

        // Indexes
        builder.HasIndex(a => new { a.Feature, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_feature");
        builder.HasIndex(a => new { a.PregnancyId, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_pregnancy");
        builder.HasIndex(a => new { a.Status, a.CreatedAt })
            .HasDatabaseName("idx_ai_logs_status");

        // Relationships
        builder.HasOne(a => a.Pregnancy)
            .WithMany(preg => preg.AiRequestLogs).HasForeignKey(a => a.PregnancyId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.User)
            .WithMany().HasForeignKey(a => a.UserId)
            .OnDelete(DeleteBehavior.SetNull);
        builder.HasOne(a => a.Template)
            .WithMany().HasForeignKey(a => a.TemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
