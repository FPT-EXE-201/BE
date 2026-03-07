namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealDayDetailDto(
    Guid Id,
    Guid MealPlanId,
    DateOnly PlanDate,
    int TotalCalories,
    List<MealItemDto> Meals
);

public record MealItemDto(
    Guid Id,
    string MealType,
    Guid? RecipeId,
    string? ItemName,
    string? PortionText,
    int? CaloriesKcal,
    string? Notes,
    List<MealItemNutrientDto> Nutrients
);

public record MealItemNutrientDto(
    string NutrientCode,
    string NutrientName,
    string Unit,
    decimal Amount
);
