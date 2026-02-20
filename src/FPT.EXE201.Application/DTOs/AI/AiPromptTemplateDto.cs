namespace FPT.EXE201.Application.DTOs.AI;

public record AiPromptTemplateDto(
    Guid Id,
    string TemplateKey,
    int Version,
    string DisplayName,
    string? Description,
    string ModelName,
    double Temperature,
    int MaxOutputTokens,
    bool IsActive,
    DateTime CreatedAt,
    DateTime UpdatedAt
);
