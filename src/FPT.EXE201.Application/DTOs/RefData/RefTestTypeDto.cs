namespace FPT.EXE201.Application.DTOs.RefData;

public record RefTestTypeDto(
    Guid Id,
    string Code,
    string? Category,
    string DisplayName,
    string? Description
);
