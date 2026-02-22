using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Cảnh báo cân nặng — tự động phát sinh khi phát hiện bất thường.
/// ⚠️ KHÔNG kế thừa BaseEntity — không soft delete.
/// WeightAlert là audit log, immutable (chỉ thêm ResolvedAt).
/// </summary>
public class WeightAlert
{
    /// <summary>Primary key.</summary>
    public Guid Id { get; set; } = Guid.NewGuid();

    /// <summary>FK → Pregnancy. CASCADE DELETE.</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Loại cảnh báo. EF HasConversion&lt;string&gt;() → lưu dạng string trong DB.</summary>
    public WeightAlertType AlertType { get; set; }

    /// <summary>Thời điểm cảnh báo được tạo.</summary>
    public DateTime TriggeredAt { get; set; } = DateTime.UtcNow;

    /// <summary>Chi tiết JSON: { currentWeight, expectedRange, weeklyGain... }.</summary>
    public string? DetailsJson { get; set; }

    /// <summary>Thời điểm cảnh báo được xử lý/giải quyết. NULL = chưa resolve.</summary>
    public DateTime? ResolvedAt { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public Pregnancy Pregnancy { get; set; } = null!;
}
