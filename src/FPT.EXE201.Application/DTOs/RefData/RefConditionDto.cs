namespace FPT.EXE201.Application.DTOs.RefData;

public record RefConditionDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description
);
