namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái của thai kỳ.
/// </summary>
public enum PregnancyStatus
{
    /// <summary>Đang mang thai</summary>
    Active,

    /// <summary>Đã kết thúc (không rõ lý do cụ thể)</summary>
    Ended,

    /// <summary>Sảy thai</summary>
    Miscarriage,

    /// <summary>Đã sinh</summary>
    Delivered
}
