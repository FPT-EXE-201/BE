using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục loại xét nghiệm (reference/master data).
/// Seed sẵn bởi hệ thống. User chọn từ danh sách khi ghi kết quả.
/// 
/// Categories:
/// - LAB: Xét nghiệm máu, nước tiểu (CBC, OGTT, HIV...)
/// - IMAGING: Chẩn đoán hình ảnh (Siêu âm, NT Scan...)
/// - OTHER: Loại khác (NST, đo huyết áp liên tục...)
/// </summary>
public class RefTestType : BaseEntity
{
    /// <summary>
    /// Mã định danh. Convention: UPPER_SNAKE_CASE.
    /// Ví dụ: "COMPLETE_BLOOD_COUNT", "ULTRASOUND", "OGTT".
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Phân nhóm xét nghiệm: "LAB", "IMAGING", "OTHER".
    /// Dùng để filter trong UI (tab Lab / Imaging / Other).
    /// </summary>
    public string? Category { get; set; }

    /// <summary>Còn sử dụng hay đã ngưng.</summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<RefTestTypeTranslation> Translations { get; set; }
        = new List<RefTestTypeTranslation>();
    public ICollection<PrenatalTest> Tests { get; set; }
        = new List<PrenatalTest>();
}
