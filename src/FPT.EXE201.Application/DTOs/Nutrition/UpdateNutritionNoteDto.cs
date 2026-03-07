using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.Nutrition;

public record UpdateNutritionNoteDto(
    NutritionNoteType? NoteType = null,
    string? ValueText = null
);
