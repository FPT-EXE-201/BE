using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
public class OcrController : BaseApiController
{
    private readonly IOcrService _ocrService;

    public OcrController(IOcrService ocrService)
    {
        _ocrService = ocrService;
    }

    /// <summary>
    /// Queue full pipeline OCR + AI Extraction cho PRENATAL_CHECKUP document.
    /// Returns 202 Accepted immediately with OcrResult (status=Pending).
    /// FE polls GET /api/ocr/{id}/status to check progress.
    /// Flow (background): Validate type → Azure OCR → RAG Context → Gemini Extraction → Structured JSON.
    /// </summary>
    [HttpPost("documents/{documentId}/ocr/process")]
    [RequirePermission("ocr.trigger")]
    public async Task<IActionResult> ProcessDocument(
        Guid documentId,
        [FromQuery] string lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _ocrService.QueueProcessAsync(documentId, userId, lang, cancellationToken);
        return Accepted(result, "OCR + AI extraction queued. Poll GET /api/ocr/{id}/status to check progress.");
    }

    /// <summary>
    /// Queue AI re-extraction (chỉ Gemini, dùng raw text đã có).
    /// Returns 202 Accepted immediately. FE polls GET /api/ocr/{id}/status.
    /// Hữu ích khi update prompt template hoặc context đã thay đổi.
    /// </summary>
    [HttpPost("ocr/{ocrResultId}/re-extract")]
    [RequirePermission("ocr.trigger")]
    public async Task<IActionResult> ReExtract(
        Guid ocrResultId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _ocrService.QueueReExtractAsync(ocrResultId, userId, cancellationToken);
        return Accepted(result, "AI re-extraction queued. Poll GET /api/ocr/{id}/status to check progress.");
    }

    /// <summary>Kiểm tra trạng thái + kết quả OCR.</summary>
    [HttpGet("ocr/{id}/status")]
    [RequirePermission("ocr.view")]
    public async Task<IActionResult> GetStatus(Guid id, CancellationToken cancellationToken)
    {
        var result = await _ocrService.GetResultAsync(id, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// Get all OCR results for a document (ordered by run number desc).
    /// Backward-compatible endpoint from Week 4.
    /// </summary>
    [HttpGet("documents/{documentId}/ocr")]
    [RequirePermission("ocr.view")]
    public async Task<IActionResult> GetByDocumentId(
        Guid documentId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var results = await _ocrService.GetByDocumentIdAsync(documentId, userId, cancellationToken);
        return Success(results);
    }
}
