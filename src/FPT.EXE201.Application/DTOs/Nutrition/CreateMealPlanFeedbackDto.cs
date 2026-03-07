namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateMealPlanFeedbackDto(
    int Rating,
    string? Comment = null
);
