using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Bản ghi cân nặng hàng ngày của thai phụ.
/// Mỗi thai kỳ chỉ cho phép 1 entry/ngày (uk_weight_logs_pregnancy_date).
/// Dùng để vẽ biểu đồ tăng cân, phát hiện bất thường, đưa khuyến nghị.
/// </summary>
public class WeightLog : BaseEntity
{
    /// <summary>FK → Pregnancy. CASCADE DELETE khi xóa thai kỳ.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Ngày ghi nhận cân nặng. Dùng DateOnly — 1 entry/day.</summary>
    public DateOnly LoggedOn { get; set; }

    /// <summary>Cân nặng (kg). DECIMAL(5,2), range: 0.01–499.99.</summary>
    public decimal WeightKg { get; set; }

    /// <summary>Ghi chú tùy chọn, max 255 chars.</summary>
    public string? Note { get; set; }

    /// <summary>Nguồn dữ liệu: Manual (user tự nhập) hoặc OCR (chụp ảnh cân → BE OCR).</summary>
    public WeightSource Source { get; set; } = WeightSource.Manual;

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Thai kỳ sở hữu bản ghi này.</summary>
    public Pregnancy Pregnancy { get; set; } = null!;
}
