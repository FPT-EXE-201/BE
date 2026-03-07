namespace FPT.EXE201.Application.DTOs.Nutrition;

public record FoodPreferenceDto(
    Guid Id,
    Guid PregnancyId,
    Guid FoodItemId,
    string FoodItemCode,
    string FoodItemDisplayName,
    string PreferenceType,
    string? Severity,
    string? Notes,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
