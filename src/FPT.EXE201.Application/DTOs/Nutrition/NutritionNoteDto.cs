namespace FPT.EXE201.Application.DTOs.Nutrition;

public record NutritionNoteDto(
    Guid Id,
    Guid PregnancyId,
    string NoteType,
    string ValueText,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
