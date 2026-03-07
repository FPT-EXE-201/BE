namespace FPT.EXE201.Application.DTOs.Nutrition;

public record RefNutrientDto(
    Guid Id,
    string Code,
    string Unit,
    string DisplayName
);
