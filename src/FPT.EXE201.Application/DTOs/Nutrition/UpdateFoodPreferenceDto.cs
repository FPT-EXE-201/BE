using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record UpdateFoodPreferenceDto(
    AllergySeverity? Severity = null,
    string? Notes = null
);
