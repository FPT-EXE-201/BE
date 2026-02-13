namespace FPT.EXE201.Application.DTOs.PrenatalTests;

public record PrenatalTestDto(
    Guid Id,
    Guid PregnancyId,
    Guid? VisitId,
    Guid TestTypeId,
    string TestTypeCode,

    /// <summary>Tên xét nghiệm theo ngôn ngữ. Ví dụ: "Công thức máu toàn phần".</summary>
    string TestTypeDisplayName,

    DateOnly TestDate,
    List<string>? ImageUrls,
    string? Notes,
    bool IsAbnormalResult,
    DateTime CreatedAt
);
