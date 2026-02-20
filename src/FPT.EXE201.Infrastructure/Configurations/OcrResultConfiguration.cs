using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Infrastructure.Configurations;

public class OcrResultConfiguration : IEntityTypeConfiguration<OcrResult>
{
    public void Configure(EntityTypeBuilder<OcrResult> builder)
    {
        builder.ToTable("ocr_results");

        builder.Property(o => o.Id).HasColumnName("id").HasColumnType("CHAR(36)");
        builder.Property(o => o.DocumentId).IsRequired().HasColumnName("document_id").HasColumnType("CHAR(36)");
        builder.Property(o => o.OcrRunNumber).IsRequired().HasColumnName("ocr_run_no").HasDefaultValue(1);
        builder.Property(o => o.Status).IsRequired().HasColumnName("status")
            .HasConversion<string>().HasMaxLength(20);
        builder.Property(o => o.OcrEngine).HasColumnName("engine").HasMaxLength(80);
        builder.Property(o => o.LanguageHint).HasColumnName("language_hint").HasMaxLength(10);
        builder.Property(o => o.RawText).HasColumnName("raw_text").HasColumnType("LONGTEXT");
        builder.Property(o => o.StructuredJson).HasColumnName("structured_json").HasColumnType("JSON");
        builder.Property(o => o.ConfidenceScore).HasColumnName("confidence").HasColumnType("DECIMAL(5,2)");
        builder.Property(o => o.ErrorMessage).HasColumnName("error_message").HasColumnType("TEXT");

        // Week 5 — AI extraction tracking columns
        builder.Property(o => o.OcrProcessingTimeMs).HasColumnName("ocr_processing_time_ms");
        builder.Property(o => o.AiModelUsed).HasColumnName("ai_model_used").HasMaxLength(50);
        builder.Property(o => o.AiTokensUsed).HasColumnName("ai_tokens_used");
        builder.Property(o => o.AiProcessingTimeMs).HasColumnName("ai_processing_time_ms");
        builder.Property(o => o.AiPromptTemplateId).HasColumnName("ai_prompt_template_id").HasColumnType("CHAR(36)");

        // WEEK 5.5: Confirm & Auto-Fill columns
        builder.Property(o => o.ConfirmedAt)
            .HasColumnName("confirmed_at")
            .HasColumnType("DATETIME(6)");

        builder.Property(o => o.ConfirmedBy)
            .HasColumnName("confirmed_by")
            .HasColumnType("CHAR(36)");

        builder.Property(o => o.ConfirmedJson)
            .HasColumnName("confirmed_json")
            .HasColumnType("JSON");

        builder.Property(o => o.AutoFillResultJson)
            .HasColumnName("auto_fill_result")
            .HasColumnType("JSON");

        builder.Property(o => o.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(o => o.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(o => o.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        builder.Ignore(o => o.IsDeleted);

        // Indexes
        builder.HasIndex(o => new { o.DocumentId, o.OcrRunNumber })
            .IsUnique().HasDatabaseName("uk_ocr_results_doc_run");
        builder.HasIndex(o => new { o.DocumentId, o.Status }).HasDatabaseName("idx_ocr_results_status");

        // Relationships
        builder.HasOne(o => o.Document)
            .WithMany(m => m.OcrResults).HasForeignKey(o => o.DocumentId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(o => o.AiPromptTemplate)
            .WithMany().HasForeignKey(o => o.AiPromptTemplateId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}
