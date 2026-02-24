namespace FPT.EXE201.Application.DTOs.WeightTracking;

public record WeightLogDto(
    Guid Id,
    Guid PregnancyId,
    DateOnly LoggedOn,
    decimal WeightKg,
    string? Note,
    string Source,
    decimal? WeightGainFromBaseline,
    DateTime CreatedAt,
    DateTime UpdatedAt,
    DateTime? DeletedAt = null
);
