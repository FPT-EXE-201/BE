using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

/// <summary>Cập nhật mức độ và ghi chú của bệnh lý đã gán. ConditionId KHÔNG thay đổi.</summary>
public record UpdatePregnancyConditionDto(
    DateOnly? DiagnosedDate,
    ConditionSeverity? Severity,
    string? Notes
);
