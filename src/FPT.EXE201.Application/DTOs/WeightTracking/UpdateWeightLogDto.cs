using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record UpdateWeightLogDto(
    decimal? WeightKg = null,
    string? Note = null,
    WeightSource? Source = null
);
