namespace FPT.EXE201.Application.DTOs.Nutrition;

public record RecipeDetailDto(
    Guid Id,
    Guid PregnancyId,
    string Title,
    string? Instructions,
    int? Servings,
    int? PrepMinutes,
    int? CookMinutes,
    DateTime CreatedAt
);
