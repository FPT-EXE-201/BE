namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Nội dung đa ngôn ngữ cho MotivationalTemplate.
/// Composite PK: (TemplateId, LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity (composite key entity).
/// </summary>
public class MotivationalTemplateTranslation
{
    /// <summary>FK → MotivationalTemplate.Id.</summary>
    public Guid TemplateId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tiêu đề ngắn (optional). Ví dụ: "Bé to bằng quả xoài!".</summary>
    public string? Title { get; set; }

    /// <summary>Nội dung chi tiết. Ví dụ: "Tuần thứ 28, bé nặng khoảng 1kg...".</summary>
    public string Message { get; set; } = string.Empty;

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public MotivationalTemplate Template { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
