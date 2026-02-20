using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.DTOs.AutoFill;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/ocr")]
public class AutoFillController : BaseApiController
{
    private readonly IAutoFillService _autoFillService;

    public AutoFillController(IAutoFillService autoFillService)
    {
        _autoFillService = autoFillService;
    }

    /// <summary>
    /// Xem dữ liệu AI đã extract — cho user review trước khi confirm.
    /// Trả về ExtractionReviewDto với form data pre-filled.
    /// </summary>
    [HttpGet("{ocrResultId}/review")]
    [RequirePermission("ocr.review")]
    public async Task<IActionResult> Review(
        Guid ocrResultId,
        [FromQuery] string lang = "vi",
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _autoFillService.ReviewAsync(ocrResultId, userId, lang, cancellationToken);
        return Success(result);
    }

    /// <summary>
    /// Confirm extracted data → auto-create PrenatalVisit/Test.
    /// User gửi dữ liệu đã review + chỉnh sửa.
    /// </summary>
    [HttpPost("{ocrResultId}/confirm")]
    [RequirePermission("ocr.confirm")]
    public async Task<IActionResult> Confirm(
        Guid ocrResultId,
        [FromBody] ConfirmExtractionDto dto,
        CancellationToken cancellationToken = default)
    {
        var userId = GetCurrentUserId();
        var result = await _autoFillService.ConfirmAsync(ocrResultId, dto, userId, cancellationToken);
        return Created(result, result.Summary);
    }
}
