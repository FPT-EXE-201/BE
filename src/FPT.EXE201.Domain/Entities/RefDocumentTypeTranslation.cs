namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Tên hiển thị đa ngôn ngữ cho loại tài liệu.
/// Composite key: (DocumentTypeId + LanguageCode).
/// ⚠️ KHÔNG kế thừa BaseEntity — entity này dùng composite primary key.
/// </summary>
public class RefDocumentTypeTranslation
{
    /// <summary>FK → RefDocumentType.Id</summary>
    public Guid DocumentTypeId { get; set; }

    /// <summary>Mã ngôn ngữ, khớp với bảng languages.code ("vi", "en").</summary>
    public string LanguageCode { get; set; } = string.Empty;

    /// <summary>Tên hiển thị cho user. Ví dụ: "Phiếu khám thai".</summary>
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>Mô tả chi tiết (optional).</summary>
    public string? Description { get; set; }

    // Navigation
    public RefDocumentType DocumentType { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
