namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Loại cảnh báo cân nặng.
/// </summary>
public enum WeightAlertType
{
    /// <summary>Tăng cân quá nhanh (>0.7 kg/week).</summary>
    RapidGain,

    /// <summary>Giảm cân quá nhanh (< -0.3 kg/week).</summary>
    RapidLoss,

    /// <summary>Tổng tăng cân vượt mức khuyến nghị tối đa.</summary>
    AboveRange,

    /// <summary>Tổng tăng cân dưới mức khuyến nghị tối thiểu.</summary>
    BelowRange
}
