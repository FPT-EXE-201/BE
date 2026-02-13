using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Request body khi tạo thai kỳ mới.
/// Cần ít nhất 1 trong 2: LastMenstrualPeriodDate hoặc EstimatedConceptionDate.
/// </summary>
public record CreatePregnancyDto(
    /// <summary>LMP — Ngày đầu kỳ kinh cuối. Dùng để tính tuổi thai và ngày dự sinh.</summary>
    DateOnly? LastMenstrualPeriodDate,

    /// <summary>Ngày thụ thai ước tính (optional).</summary>
    DateOnly? EstimatedConceptionDate,

    /// <summary>Ghi chú tự do.</summary>
    string? Notes,

    // ── Nhóm 1: Thông tin bé ──
    /// <summary>Biệt danh bé. Ví dụ: "Bé Bông", "Cherry".</summary>
    string? BabyNickname = null,

    /// <summary>Giới tính em bé. Mặc định Unknown.</summary>
    BabyGender BabyGender = BabyGender.Unknown,

    /// <summary>Loại thai: Singleton / Twins / Triplets / Other.</summary>
    PregnancyType PregnancyType = PregnancyType.Singleton,

    // ── Nhóm 2: Y tế mẹ ──
    /// <summary>Nhóm máu mẹ. Ví dụ: "A+", "O-".</summary>
    string? MotherBloodType = null,

    /// <summary>Cân nặng trước mang thai (kg). Baseline cho Weight Tracking.</summary>
    decimal? PrePregnancyWeightKg = null,

    /// <summary>Chiều cao mẹ (cm). Dùng tính BMI.</summary>
    decimal? HeightCm = null,

    // ── Nhóm 3: Thai sản chuyên sâu ──
    /// <summary>Nguồn tính ngày dự sinh.</summary>
    DueDateSource DueDateSource = DueDateSource.LMP,

    /// <summary>Gravida — tổng số lần mang thai (tính cả lần này). Ví dụ: G2.</summary>
    int? Gravida = null,

    /// <summary>Para — tổng số lần sinh trước đó. Ví dụ: P1.</summary>
    int? Para = null,

    /// <summary>Ảnh cover cho hồ sơ thai kỳ (URL).</summary>
    string? CoverImageUrl = null
);
