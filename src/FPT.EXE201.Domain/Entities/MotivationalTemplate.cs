using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Template nội dung động viên cho mẹ bầu — theo tuần thai.
/// 3 categories: BabySize (so sánh kích thước bé), Milestone (cột mốc), Tip (mẹo hay).
/// Admin quản lý nội dung. User nhận nội dung phù hợp với tuần thai hiện tại.
/// </summary>
public class MotivationalTemplate : BaseEntity
{
    /// <summary>Danh mục: BabySize, Milestone, Tip — lưu dạng string.</summary>
    public MotivationalCategory Category { get; set; } = MotivationalCategory.BabySize;

    /// <summary>Tuần thai bắt đầu áp dụng (inclusive, 0-45).</summary>
    public int WeekStart { get; set; }

    /// <summary>Tuần thai kết thúc áp dụng (inclusive, 0-45, >= WeekStart).</summary>
    public int WeekEnd { get; set; }

    /// <summary>Template có đang active không.</summary>
    public bool IsActive { get; set; } = true;

    /// <summary>Variables JSON cho template (keys mà FE có thể thay thế).</summary>
    public string? VariablesJson { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    /// <summary>Nội dung đa ngôn ngữ.</summary>
    public ICollection<MotivationalTemplateTranslation> Translations { get; set; }
        = new List<MotivationalTemplateTranslation>();
}
