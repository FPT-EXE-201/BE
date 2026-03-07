namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealPlanFeedbackDto(
    Guid Id,
    Guid MealPlanId,
    Guid UserId,
    int Rating,
    string? Comment,
    DateTime CreatedAt
);
