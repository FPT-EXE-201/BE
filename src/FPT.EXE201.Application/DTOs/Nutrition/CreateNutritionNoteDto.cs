using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record CreateNutritionNoteDto(
    NutritionNoteType NoteType,
    string ValueText
);
