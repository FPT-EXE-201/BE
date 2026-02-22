namespace FPT.EXE201.Application.DTOs.WeightTracking;

/// <summary>
/// Kết quả trích xuất cân nặng từ ảnh — trả về cho FE để user confirm.
/// </summary>
public record WeightOcrExtractResultDto(
    bool Success,
    decimal? ExtractedWeightKg,
    string? RawOcrText,
    decimal? ConfidenceScore,
    string Message
);
