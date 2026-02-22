namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightGoalDto(
    Guid Id,
    Guid PregnancyId,
    decimal? HeightCm,
    decimal? PrePregnancyWeightKg,
    decimal? Bmi,
    string? BmiCategory,
    decimal? RecommendedTotalGainMin,
    decimal? RecommendedTotalGainMax,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
