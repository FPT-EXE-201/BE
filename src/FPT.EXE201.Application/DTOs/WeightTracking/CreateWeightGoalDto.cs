namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record CreateWeightGoalDto(
    decimal? HeightCm = null,
    decimal? PrePregnancyWeightKg = null,
    decimal? RecommendedTotalGainMin = null,
    decimal? RecommendedTotalGainMax = null,
    string? Notes = null
);
