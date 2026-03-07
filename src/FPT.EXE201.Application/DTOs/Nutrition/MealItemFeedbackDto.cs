namespace FPT.EXE201.Application.DTOs.Nutrition;

public record MealItemFeedbackDto(
    Guid Id,
    Guid MealItemId,
    Guid UserId,
    bool Liked,
    string? Comment,
    DateTime CreatedAt
);
