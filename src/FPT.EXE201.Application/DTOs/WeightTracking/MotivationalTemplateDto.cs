namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record MotivationalTemplateDto(
    Guid Id,
    string Category,
    int WeekStart,
    int WeekEnd,
    string? VariablesJson,
    string? Title,
    string Message
);
