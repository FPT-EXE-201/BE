namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateMealItemFeedbackDto(
    bool Liked,
    string? Comment = null
);
