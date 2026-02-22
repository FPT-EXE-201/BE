namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightChartDataDto(
    decimal? PrePregnancyWeightKg,
    decimal? RecommendedGainMin,
    decimal? RecommendedGainMax,
    decimal? CurrentWeightKg,
    decimal? TotalGainKg,
    int TotalEntries,
    List<WeightChartPointDto> DataPoints
);

public record WeightChartPointDto(
    DateOnly Date,
    decimal WeightKg,
    int? GestationalWeek
);
