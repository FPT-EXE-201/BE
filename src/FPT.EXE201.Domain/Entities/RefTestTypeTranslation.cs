namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho loại xét nghiệm.
/// Composite key: (TestTypeId + LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity.
/// </summary>
public class RefTestTypeTranslation
{
    /// <summary>FK → RefTestType.Id</summary>
    public Guid TestTypeId { get; set; }

    /// <summary>Mã ngôn ngữ ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị. Ví dụ: "Công thức máu toàn phần".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional).</summary>
    public string? Description { get; set; }

    // Navigation
    public RefTestType TestType { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
