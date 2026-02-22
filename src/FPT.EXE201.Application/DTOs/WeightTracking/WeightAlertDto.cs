namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightAlertDto(
    Guid Id,
    Guid PregnancyId,
    string AlertType,
    DateTime TriggeredAt,
    string? DetailsJson,
    DateTime? ResolvedAt,
    bool IsResolved
);
