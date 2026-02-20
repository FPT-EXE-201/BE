using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Versioned prompt template cho AI features.
/// Chứa 3 rule layers (System, Domain, Feature) + output schema + model config.
/// Dùng chung cho: Medical Record Extraction, Nutrition Planning, Chat, etc.
///
/// Rule Layer System:
///   Layer 1 (SystemRules)  — Ngôn ngữ, format JSON, safety constraints
///   Layer 2 (DomainRules)  — Pregnancy domain knowledge, Vietnamese medical terminology (shared)
///   Layer 3 (FeatureRules) — Feature-specific instructions (extraction schema, meal plan, chat)
///
/// PromptBuilder.FromTemplate(template) sẽ lắp ráp 3 layers + OutputSchema → SystemMessage.
/// </summary>
public class AiPromptTemplate : BaseEntity
{
    /// <summary>
    /// Unique key cho template. Ví dụ: "medical_record.extraction", "nutrition.meal_planning".
    /// Kết hợp với Version tạo unique constraint.
    /// </summary>
    public string TemplateKey { get; set; } = null!;

    /// <summary>Version number. Cho phép A/B test hoặc rollback prompt.</summary>
    public int Version { get; set; } = 1;

    /// <summary>Tên hiển thị cho admin UI.</summary>
    public string DisplayName { get; set; } = null!;

    /// <summary>Mô tả mục đích của template.</summary>
    public string? Description { get; set; }

    // ═══ Rule Layers (stored as text, assembled by PromptBuilder) ═══

    /// <summary>Layer 1: System rules — language, format, safety constraints.</summary>
    public string SystemRules { get; set; } = null!;

    /// <summary>Layer 2: Domain rules — pregnancy medicine, terminology. Shared across features.</summary>
    public string? DomainRules { get; set; }

    /// <summary>Layer 3: Feature-specific rules — extraction schema, meal planning guidelines.</summary>
    public string FeatureRules { get; set; } = null!;

    /// <summary>JSON schema cho expected AI output. Giúp Gemini conform to structure.</summary>
    public string? OutputSchema { get; set; }

    // ═══ Model Configuration ═══

    /// <summary>Tên model AI. Default: gemini-2.5-flash (balance speed/quality).</summary>
    public string ModelName { get; set; } = "gemini-2.5-flash";

    /// <summary>Temperature: 0.0 = deterministic, 1.0 = creative. Extraction nên dùng 0.1.</summary>
    public double Temperature { get; set; } = 0.1;

    /// <summary>Max tokens cho AI response.</summary>
    public int MaxOutputTokens { get; set; } = 8192;

    /// <summary>Template có đang active không. Chỉ active version mới nhất được sử dụng.</summary>
    public bool IsActive { get; set; } = true;
}
