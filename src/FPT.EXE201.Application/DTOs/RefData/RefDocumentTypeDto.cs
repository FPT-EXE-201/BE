namespace FPT.EXE201.Application.DTOs.RefData;

public record RefDocumentTypeDto(
    Guid Id,
    string Code,
    string DisplayName,
    string? Description
);
