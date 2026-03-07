namespace FPT.EXE201.Application.DTOs.Nutrition;

/// <summary>
/// Status DTO for polling meal plan generation progress.
/// FE polls GET /api/meal-plans/{id}/status every 3-5s until Succeeded/Failed.
/// </summary>
public record MealPlanStatusDto(
    Guid Id,
    Guid PregnancyId,
    string Status,
    int CompletedWeeks,
    int TotalWeeks,
    string? Title,
    string? ErrorMessage,
    DateTime CreatedAt
);
