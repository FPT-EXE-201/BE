namespace FPT.EXE201.Application.DTOs.PregnancyConditions;

public record PregnancyConditionDto(
    Guid Id,
    Guid PregnancyId,
    Guid ConditionId,

    /// <summary>Mã bệnh lý. Ví dụ: "GESTATIONAL_DIABETES".</summary>
    string ConditionCode,

    /// <summary>Tên hiển thị theo ngôn ngữ. Ví dụ: "Tiểu đường thai kỳ".</summary>
    string ConditionDisplayName,

    /// <summary>Mô tả chi tiết theo ngôn ngữ.</summary>
    string? ConditionDescription,

    DateOnly? DiagnosedDate,
    string? Severity,
    string? Notes,
    DateTime CreatedAt
);
