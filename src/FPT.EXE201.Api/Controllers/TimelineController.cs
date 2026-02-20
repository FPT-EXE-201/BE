using Microsoft.AspNetCore.Mvc;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Application.Authorization;

namespace FPT.EXE201.Api.Controllers;

[Route("api/pregnancies/{pregnancyId}/timeline")]
public class TimelineController : BaseApiController
{
    private readonly IMedicalDocumentService _documentService;

    public TimelineController(IMedicalDocumentService documentService)
    {
        _documentService = documentService;
    }

    /// <summary>Dòng thời gian thai kỳ (documents + visits).</summary>
    [HttpGet]
    [RequirePermission("document.view")]
    public async Task<IActionResult> GetTimeline(
        Guid pregnancyId, CancellationToken cancellationToken)
    {
        var userId = GetCurrentUserId();
        var result = await _documentService.GetTimelineAsync(pregnancyId, userId, cancellationToken);
        return Success(result);
    }
}
