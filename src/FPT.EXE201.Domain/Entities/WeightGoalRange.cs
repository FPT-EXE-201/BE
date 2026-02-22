using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Mục tiêu tăng cân cho thai kỳ — dựa trên IOM guidelines.
/// 1 record per pregnancy (unique key uk_weight_goals_pregnancy).
/// Auto-calculate BMI từ height + pre-pregnancy weight.
/// </summary>
public class WeightGoalRange : BaseEntity
{
    /// <summary>FK → Pregnancy (unique).</summary>
    public Guid PregnancyId { get; set; }

    /// <summary>Chiều cao mẹ (cm). Copy từ Pregnancy.HeightCm hoặc user nhập lại.</summary>
    public decimal? HeightCm { get; set; }

    /// <summary>Cân nặng trước mang thai (kg). Copy từ Pregnancy.PrePregnancyWeightKg.</summary>
    public decimal? PrePregnancyWeightKg { get; set; }

    /// <summary>BMI trước mang thai. Auto-calculated: weight / (height/100)².</summary>
    public decimal? Bmi { get; set; }

    /// <summary>Mức tăng cân tối thiểu khuyến nghị (kg) — theo IOM guidelines.</summary>
    public decimal? RecommendedTotalGainMin { get; set; }

    /// <summary>Mức tăng cân tối đa khuyến nghị (kg) — theo IOM guidelines.</summary>
    public decimal? RecommendedTotalGainMax { get; set; }

    /// <summary>Ghi chú (bác sĩ tư vấn, ghi nhận đặc biệt).</summary>
    public string? Notes { get; set; }

    // ══════════════════════════════════════
    // Navigation properties
    // ══════════════════════════════════════

    public Pregnancy Pregnancy { get; set; } = null!;
}
