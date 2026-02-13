namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho bệnh lý thai kỳ.
/// Composite key: (ConditionId + LanguageCode).
/// 
/// Ví dụ:
///   ConditionId=xxx, lang="vi" → "Tiểu đường thai kỳ"
///   ConditionId=xxx, lang="en" → "Gestational Diabetes"
///   
/// ⚠️ KHÔNG kế thừa BaseEntity — entity này dùng composite primary key.
/// </summary>
public class RefPregnancyConditionTranslation
{
    /// <summary>FK → RefPregnancyCondition.Id</summary>
    public Guid ConditionId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị cho user. Ví dụ: "Tiểu đường thai kỳ".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional). Hiển thị khi user tap xem thêm.</summary>
    public string? Description { get; set; }

    // Navigation
    public RefPregnancyCondition Condition { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
