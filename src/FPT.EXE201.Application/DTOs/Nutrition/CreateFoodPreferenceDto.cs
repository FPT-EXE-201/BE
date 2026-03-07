using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateFoodPreferenceDto(
    Guid FoodItemId,
    FoodPreferenceType PreferenceType,
    AllergySeverity? Severity = null,
    string? Notes = null
);
