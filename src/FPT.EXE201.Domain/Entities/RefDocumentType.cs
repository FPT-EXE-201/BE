using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục loại tài liệu y tế (reference/master data).
/// Seed sẵn bởi hệ thống. User chọn từ danh sách khi upload.
/// Cũng dùng làm vocabulary cho Gemini AI classification.
/// 
/// Ví dụ: PRENATAL_CHECKUP, ULTRASOUND, BLOOD_TEST, PRESCRIPTION...
/// </summary>
public class RefDocumentType : BaseEntity
{
    /// <summary>
    /// Mã định danh duy nhất. Convention: UPPER_SNAKE_CASE.
    /// Ví dụ: "PRENATAL_CHECKUP", "ULTRASOUND".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Còn sử dụng hay đã ngưng.
    /// false = ẩn khỏi dropdown nhưng giữ data cũ.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    /// <summary>Tên hiển thị theo từng ngôn ngữ (VI, EN...).</summary>
    public ICollection<RefDocumentTypeTranslation> Translations { get; set; }
        = new List<RefDocumentTypeTranslation>();

    /// <summary>Các tài liệu thuộc loại này.</summary>
    public ICollection<MedicalDocument> Documents { get; set; }
        = new List<MedicalDocument>();
}
