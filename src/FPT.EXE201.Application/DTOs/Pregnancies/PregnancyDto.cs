namespace FPT.EXE201.Application.DTOs.Pregnancies;

/// <summary>
/// Response trả về thông tin thai kỳ.
/// </summary>
public record PregnancyDto(
    Guid Id,
    Guid UserId,
    int PregnancyNumber,
    string Status,
    DateOnly? LastMenstrualPeriodDate,
    DateOnly? ExpectedDeliveryDate,
    DateOnly? EstimatedConceptionDate,
    int? CurrentGestationalWeek,

    /// <summary>Tuổi thai dạng hiển thị. Ví dụ: "28w3d" (28 tuần 3 ngày).</summary>
    string? GestationalAgeDisplay,

    string? Notes,

    // Nhóm 1: Thông tin bé
    string? BabyNickname,
    string BabyGender,
    string PregnancyType,

    // Nhóm 2: Y tế mẹ
    string? MotherBloodType,
    decimal? PrePregnancyWeightKg,
    decimal? HeightCm,

    /// <summary>BMI trước mang thai. Auto-computed: weight / (height/100)^2. Null nếu thiếu dữ liệu.</summary>
    decimal? PrePregnancyBmi,

    // Nhóm 3: Thai sản chuyên sâu
    string DueDateSource,
    int? Gravida,
    int? Para,

    /// <summary>Hiển thị tiện kỷ hiệu y khoa. Ví dụ: "G2P1".</summary>
    string? ObstetricFormula,

    DateOnly? ActualDeliveryDate,
    string? DeliveryMethod,
    string? CoverImageUrl,

    DateTime CreatedAt,
    DateTime UpdatedAt
);
