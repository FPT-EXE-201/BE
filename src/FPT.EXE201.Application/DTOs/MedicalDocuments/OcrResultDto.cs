namespace FPT.EXE201.Application.DTOs.MedicalDocuments;

/// <summary>
/// Response trả về trạng thái + kết quả OCR.
/// ⚠️ Dùng class (không dùng positional record) vì AutoMapper cần parameterless constructor + settable properties.
/// </summary>
public class OcrResultDto
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public int OcrRunNumber { get; set; }
    public string Status { get; set; } = null!;
    public string? OcrEngine { get; set; }
    public string? LanguageHint { get; set; }
    public string? RawText { get; set; }
    public string? StructuredJson { get; set; }
    public decimal? ConfidenceScore { get; set; }
    public string? ErrorMessage { get; set; }

    // Week 5: AI Processing Fields
    public int? OcrProcessingTimeMs { get; set; }
    public string? AiModelUsed { get; set; }
    public int? AiTokensUsed { get; set; }
    public int? AiProcessingTimeMs { get; set; }
    public Guid? AiPromptTemplateId { get; set; }

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // WEEK 5.5: Confirm fields
    public DateTime? ConfirmedAt { get; set; }
    public Guid? ConfirmedBy { get; set; }
    public string? ConfirmedJson { get; set; }
    public string? AutoFillResultJson { get; set; }
}
