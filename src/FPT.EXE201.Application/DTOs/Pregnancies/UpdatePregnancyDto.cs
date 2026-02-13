using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Pregnancies;

public record UpdatePregnancyDto(
    DateOnly? LastMenstrualPeriodDate,
    DateOnly? EstimatedConceptionDate,
    string? Notes,

    // Nhóm 1
    string? BabyNickname,
    BabyGender? BabyGender,
    PregnancyType? PregnancyType,

    // Nhóm 2
    string? MotherBloodType,
    decimal? PrePregnancyWeightKg,
    decimal? HeightCm,

    // Nhóm 3
    DueDateSource? DueDateSource,
    /// <summary>Nếu FE truyền EDD mới (bác sĩ điều chỉnh theo ultrasound), cập nhật luôn.</summary>
    DateOnly? ExpectedDeliveryDate,
    int? Gravida,
    int? Para,
    string? CoverImageUrl
);
