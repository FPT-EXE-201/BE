using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Ghi nhận một bệnh lý cụ thể cho một thai kỳ cụ thể.
/// Ví dụ: Thai kỳ #1 của Lan được chẩn đoán Tiểu đường thai kỳ vào ngày 15/06.
/// 
/// Business rules:
/// - Mỗi condition chỉ được gán 1 lần per pregnancy (unique: pregnancy_id + condition_id).
/// - Soft delete khi bác sĩ xác nhận chẩn đoán sai.
/// </summary>
public class PregnancyCondition : BaseEntity
{
    /// <summary>FK → Pregnancy. Thai kỳ được chẩn đoán bệnh lý này.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>FK → RefPregnancyCondition. Loại bệnh lý (từ danh mục master).</summary>
    public Guid ConditionId { get; set; }

    /// <summary>
    /// Ngày được chẩn đoán bệnh lý này.
    /// Nullable vì user có thể chưa nhớ chính xác ngày.
    /// </summary>
    public DateOnly? DiagnosedDate { get; set; }

    /// <summary>
    /// Mức độ nghiêm trọng: Mild / Moderate / Severe.
    /// Nullable vì lúc mới phát hiện có thể chưa đánh giá mức độ.
    /// </summary>
    public ConditionSeverity? Severity { get; set; }

    /// <summary>Ghi chú thêm của user hoặc bác sĩ.</summary>
    public string? Notes { get; set; }

    // Navigation
    /// <summary>Thai kỳ sở hữu condition này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;

    /// <summary>Thông tin bệnh lý từ danh mục master (code, translations).</summary>
    public RefPregnancyCondition Condition { get; set; } = null!;
}
