namespace FPT.EXE201.Application.AI.Models;

/// <summary>
/// Response từ OCR provider.
/// </summary>
public record OcrResponse(
    /// <summary>Raw text đã trích xuất từ ảnh/PDF.</summary>
    string RawText,

    /// <summary>Confidence score trung bình (0.00 - 100.00). Đồng bộ với OcrResult.ConfidenceScore DECIMAL(5,2).</summary>
    decimal ConfidenceScore,

    /// <summary>Thời gian xử lý.</summary>
    TimeSpan ProcessingTime,

    /// <summary>Engine đã sử dụng (e.g., "azure-document-intelligence-4.0").</summary>
    string EngineUsed
);
