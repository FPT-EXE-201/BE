using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record CreateWeightLogDto(
    DateOnly LoggedOn,
    decimal WeightKg,
    string? Note = null,
    WeightSource Source = WeightSource.Manual
);
