using FPT.EXE201.Application.AI.ExtractionModels;
using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý full pipeline: OCR → AI Extraction cho medical records.
/// ⚠️ CHỈ áp dụng cho PRENATAL_CHECKUP documents. Các loại khác sẽ bị reject.
/// Dùng RAG pattern để inject pregnancy context vào AI prompt.
/// </summary>
public interface IMedicalRecordAiService
{
    /// <summary>
    /// Chạy full pipeline cho 1 PRENATAL_CHECKUP document:
    /// 1. Validate DocumentType == PRENATAL_CHECKUP
    /// 2. Download file từ storage
    /// 3. Azure OCR → raw text
    /// 4. Retrieve pregnancy context (RAG)
    /// 5. Build prompt (Rule Layers + Context)
    /// 6. Gemini extraction → structured JSON
    /// 7. Save kết quả vào OcrResult
    /// ⚠️ Throws BadRequestException nếu DocumentType != PRENATAL_CHECKUP
    /// </summary>
    Task<OcrResultDto> ProcessDocumentAsync(
        Guid documentId, Guid currentUserId,
        string? languageHint = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Chạy lại extraction (chỉ Gemini, dùng raw text đã có) với template mới hoặc context mới.
    /// </summary>
    Task<OcrResultDto> ReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
