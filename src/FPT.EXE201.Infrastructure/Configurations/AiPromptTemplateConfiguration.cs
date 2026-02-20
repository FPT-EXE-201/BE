using FPT.EXE201.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace FPT.EXE201.Infrastructure.Configurations;

public class AiPromptTemplateConfiguration : IEntityTypeConfiguration<AiPromptTemplate>
{
    public void Configure(EntityTypeBuilder<AiPromptTemplate> builder)
    {
        builder.ToTable("ai_prompt_templates");

        // Primary Key — CHAR(36), KHÔNG BINARY(16)
        builder.HasKey(e => e.Id);
        builder.Property(e => e.Id)
            .HasColumnName("id")
            .HasColumnType("CHAR(36)");

        // Properties
        builder.Property(e => e.TemplateKey)
            .IsRequired()
            .HasColumnName("template_key")
            .HasMaxLength(100);

        builder.Property(e => e.Version)
            .HasColumnName("version")
            .HasDefaultValue(1);

        builder.Property(e => e.DisplayName)
            .IsRequired()
            .HasColumnName("display_name")
            .HasMaxLength(200);

        builder.Property(e => e.Description)
            .HasColumnName("description")
            .HasColumnType("TEXT");

        // Rule Layers
        builder.Property(e => e.SystemRules)
            .IsRequired()
            .HasColumnName("system_rules")
            .HasColumnType("TEXT");

        builder.Property(e => e.DomainRules)
            .HasColumnName("domain_rules")
            .HasColumnType("TEXT");

        builder.Property(e => e.FeatureRules)
            .IsRequired()
            .HasColumnName("feature_rules")
            .HasColumnType("TEXT");

        builder.Property(e => e.OutputSchema)
            .HasColumnName("output_schema")
            .HasColumnType("TEXT");

        // Model Configuration
        builder.Property(e => e.ModelName)
            .IsRequired()
            .HasColumnName("model_name")
            .HasMaxLength(50)
            .HasDefaultValue("gemini-2.5-flash");

        builder.Property(e => e.Temperature)
            .HasColumnName("temperature")
            .HasColumnType("DECIMAL(3,2)")
            .HasDefaultValue(0.1);

        builder.Property(e => e.MaxOutputTokens)
            .HasColumnName("max_output_tokens")
            .HasDefaultValue(8192);

        builder.Property(e => e.IsActive)
            .HasColumnName("is_active")
            .HasDefaultValue(true);

        // BaseEntity timestamps
        builder.Property(e => e.CreatedAt).HasColumnName("created_at").HasColumnType("DATETIME(6)");
        builder.Property(e => e.UpdatedAt).HasColumnName("updated_at").HasColumnType("DATETIME(6)");
        builder.Property(e => e.DeletedAt).HasColumnName("deleted_at").HasColumnType("DATETIME(6)");

        // LUÔN ignore computed property
        builder.Ignore(e => e.IsDeleted);

        // Unique constraint: template_key + version
        builder.HasIndex(e => new { e.TemplateKey, e.Version })
            .IsUnique()
            .HasDatabaseName("uk_ai_templates_key_version");
    }
}
