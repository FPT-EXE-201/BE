namespace FPT.EXE201.Application.DTOs.Nutrition;

public record GenerateMealPlanDto(
    DateOnly StartDate,
    int DurationWeeks,
    string? AdditionalNotes = null
);
