namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanSummaryDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly StartDate,
    DateOnly EndDate,
    string Source,
    string Status,
    string? Title,
    int TotalDays,
    DateTime CreatedAt
);
