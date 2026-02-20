using FPT.EXE201.Application.DTOs.PrenatalVisits.VitalsJson;

namespace FPT.EXE201.Application.DTOs.AutoFill;

/// <summary>
/// Response trả về dữ liệu AI đã extract cho user review.
/// Flutter sẽ hiển thị form pre-filled từ data này.
/// User edit rồi gửi lại qua ConfirmExtractionDto.
/// </summary>
public class ExtractionReviewDto
{
    public Guid OcrResultId { get; set; }
    public Guid DocumentId { get; set; }
    public Guid PregnancyId { get; set; }

    /// <summary>Document type ID (FK → RefDocumentType). FE dùng luôn khi gọi POST confirm.</summary>
    public Guid? DocumentTypeId { get; set; }

    /// <summary>Document type code: PRENATAL_CHECKUP, BLOOD_TEST, etc.</summary>
    public string? DocumentTypeCode { get; set; }

    /// <summary>Document type display name (theo ngôn ngữ).</summary>
    public string? DocumentTypeDisplayName { get; set; }

    /// <summary>OCR status hiện tại.</summary>
    public string Status { get; set; } = null!;

    /// <summary>Confidence score từ OCR (0-100%).</summary>
    public decimal? ConfidenceScore { get; set; }

    /// <summary>URLs ảnh gốc của document (để FE hiển thị ảnh khi review).</summary>
    public List<string> FileUrls { get; set; } = new();

    // ═══ Extracted Data (parsed từ StructuredJson → MedicalRecordExtractionResult) ═══

    /// <summary>
    /// Dữ liệu VitalsJson (cho PRENATAL_CHECKUP).
    /// Parse trực tiếp từ MedicalRecordExtractionResult.VitalsData — ĐÃ LÀ VitalsJsonDto.
    /// Null nếu AI không extract được.
    /// </summary>
    public VitalsJsonDto? Vitals { get; set; }

    /// <summary>Độ tin cậy tổng thể của AI extraction (0.0 - 1.0).</summary>
    public double? OverallConfidence { get; set; }

    /// <summary>Raw StructuredJson (cho debug/advanced users).</summary>
    public string? RawStructuredJson { get; set; }

    /// <summary>Có thể auto-fill hay không (dựa vào documentType + extraction quality).</summary>
    public bool CanAutoFill { get; set; }

    /// <summary>Lý do không thể auto-fill (nếu CanAutoFill = false).</summary>
    public string? CannotAutoFillReason { get; set; }
}
