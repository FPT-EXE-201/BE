namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanDetailDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Source,
    string? Title,
    string? Notes,
    List<MealPlanDaySummaryDto> Days,
    DateTime CreatedAt,
    DateTime UpdatedAt
);

public record MealPlanDaySummaryDto(
    Guid Id,
    DateOnly PlanDate,
    int TotalCalories,
    int MealCount
);
