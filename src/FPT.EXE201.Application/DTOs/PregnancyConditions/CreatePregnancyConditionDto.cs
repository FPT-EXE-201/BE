using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

public record CreatePregnancyConditionDto(
    /// <summary>ID của bệnh lý từ danh mục (ref_pregnancy_conditions).</summary>
    Guid ConditionId,

    /// <summary>Ngày được chẩn đoán.</summary>
    DateOnly? DiagnosedDate,

    /// <summary>Mức độ: Mild / Moderate / Severe.</summary>
    ConditionSeverity? Severity,

    string? Notes
);
