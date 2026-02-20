using FPT.EXE201.Application.DTOs.MedicalDocuments;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service xử lý OCR. Week 4 = stub. Full implementation in later weeks.
/// ⚠️ Interface ở Application, Implementation ở Infrastructure.
/// </summary>
public interface IOcrService
{
    /// <summary>Tạo OcrResult mới với status=Pending, queue để xử lý.</summary>
    Task<Guid> QueueOcrAsync(
        Guid documentId, string? languageHint = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue full OCR+AI pipeline for manual trigger.
    /// Creates OcrResult (Pending), enqueues background job, returns OcrResultDto immediately.
    /// FE polls GET /api/ocr/{id}/status to check progress.
    /// </summary>
    Task<OcrResultDto> QueueProcessAsync(
        Guid documentId, Guid currentUserId, string? languageHint = "vi",
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Queue AI re-extraction (skip OCR, reuse existing rawText).
    /// Sets OcrResult to AiExtracting, enqueues background job, returns OcrResultDto immediately.
    /// FE polls GET /api/ocr/{id}/status to check progress.
    /// </summary>
    Task<OcrResultDto> QueueReExtractAsync(
        Guid ocrResultId, Guid currentUserId,
        CancellationToken cancellationToken = default);

    /// <summary>Lấy kết quả OCR theo ID.</summary>
    Task<OcrResultDto> GetResultAsync(
        Guid ocrResultId, CancellationToken cancellationToken = default);

    /// <summary>Get all OCR results for a document (ordered by run number desc).</summary>
    Task<List<OcrResultDto>> GetByDocumentIdAsync(
        Guid documentId, Guid currentUserId,
        CancellationToken cancellationToken = default);
}
