using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Danh mục bệnh lý thai kỳ (reference/master data).
/// Đây là bảng lookup — seed sẵn bởi hệ thống, admin quản lý.
/// User KHÔNG tạo mới, chỉ CHỌN từ danh sách này để gán vào thai kỳ.
/// 
/// Ví dụ: GESTATIONAL_DIABETES, PREECLAMPSIA, ANEMIA...
/// </summary>
public class RefPregnancyCondition : BaseEntity
{
    /// <summary>
    /// Mã định danh duy nhất, dùng trong logic nghiệp vụ.
    /// Convention: UPPER_SNAKE_CASE. Ví dụ: "GESTATIONAL_DIABETES".
    /// Không được thay đổi sau khi đã có data reference.
    /// </summary>
    public string Code { get; set; } = string.Empty;

    /// <summary>
    /// Cho phép hiển thị trong dropdown hay không.
    /// false = đã ngưng sử dụng nhưng giữ lại cho data cũ.
    /// </summary>
    public bool IsActive { get; set; } = true;

    // Navigation
    /// <summary>Tên hiển thị theo từng ngôn ngữ (VI, EN...).</summary>
    public ICollection<RefPregnancyConditionTranslation> Translations { get; set; }
        = new List<RefPregnancyConditionTranslation>();

    /// <summary>Các thai kỳ đã được chẩn đoán bệnh lý này.</summary>
    public ICollection<PregnancyCondition> PregnancyConditions { get; set; }
        = new List<PregnancyCondition>();
}
