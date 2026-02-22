using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
[Authorize]
public class WeightLogsController : BaseApiController
{
    private readonly IWeightLogService _weightLogService;

    public WeightLogsController(IWeightLogService weightLogService)
    {
        _weightLogService = weightLogService;
    }

    // ═══ OCR Weight Extraction ═══

    /// <summary>
    /// Upload ảnh chụp cân → OCR trích xuất cân nặng → trả về cho FE confirm.
    /// </summary>
    [HttpPost("pregnancies/{pregnancyId:guid}/weight-logs/extract-weight")]
    [RequirePermission("weight_log.write")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> ExtractWeight(
        Guid pregnancyId, IFormFile image, CancellationToken ct)
    {
        if (image == null || image.Length == 0)
            throw new BadRequestException("Image file is required.");

        var allowedExtensions = new[] { ".jpg", ".jpeg", ".png" };
        var extension = Path.GetExtension(image.FileName).ToLowerInvariant();
        if (!allowedExtensions.Contains(extension))
            throw new BadRequestException("Only JPEG and PNG images are allowed.");

        if (image.Length > 5 * 1024 * 1024)
            throw new BadRequestException("Image size must not exceed 5 MB.");

        using var stream = image.OpenReadStream();
        var result = await _weightLogService.ExtractWeightFromImageAsync(
            pregnancyId, GetCurrentUserId(), stream, image.FileName, ct);

        return Success(result, result.Message);
    }

    // ═══ Weight Logs ═══

    [HttpPost("pregnancies/{pregnancyId:guid}/weight-logs")]
    [RequirePermission("weight_log.write")]
    public async Task<IActionResult> Create(
        Guid pregnancyId, [FromBody] CreateWeightLogDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.CreateAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Weight log recorded successfully");
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-logs")]
    [RequirePermission("weight_log.read")]
    public async Task<IActionResult> GetByPregnancy(
        Guid pregnancyId, [FromQuery] QueryOptions options, CancellationToken ct)
    {
        var result = await _weightLogService.GetByPregnancyIdPagedAsync(
            pregnancyId, GetCurrentUserId(), options, ct);
        return Success(result);
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-logs/chart")]
    [RequirePermission("weight_log.read")]
    public async Task<IActionResult> GetChartData(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetChartDataAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-logs/{id:guid}")]
    [RequirePermission("weight_log.write")]
    public async Task<IActionResult> Update(
        Guid id, [FromBody] UpdateWeightLogDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.UpdateAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Weight log updated successfully");
    }

    [HttpDelete("weight-logs/{id:guid}")]
    [RequirePermission("weight_log.delete")]
    public async Task<IActionResult> Delete(Guid id, CancellationToken ct)
    {
        await _weightLogService.DeleteAsync(id, GetCurrentUserId(), ct);
        return Success<object?>(null, "Weight log deleted successfully");
    }

    // ═══ Weight Goals ═══

    [HttpPost("pregnancies/{pregnancyId:guid}/weight-goals")]
    [RequirePermission("weight_goal.write")]
    public async Task<IActionResult> CreateGoal(
        Guid pregnancyId, [FromBody] CreateWeightGoalDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.CreateGoalAsync(pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Weight goal set successfully");
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-goals")]
    [RequirePermission("weight_goal.read")]
    public async Task<IActionResult> GetGoal(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetGoalAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-goals/{id:guid}")]
    [RequirePermission("weight_goal.write")]
    public async Task<IActionResult> UpdateGoal(
        Guid id, [FromBody] CreateWeightGoalDto dto, CancellationToken ct)
    {
        var result = await _weightLogService.UpdateGoalAsync(id, GetCurrentUserId(), dto, ct);
        return Success(result, "Weight goal updated successfully");
    }

    // ═══ Weight Alerts ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/weight-alerts")]
    [RequirePermission("weight_alert.read")]
    public async Task<IActionResult> GetAlerts(Guid pregnancyId, CancellationToken ct)
    {
        var result = await _weightLogService.GetAlertsAsync(pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPut("weight-alerts/{id:guid}/resolve")]
    [RequirePermission("weight_alert.resolve")]
    public async Task<IActionResult> ResolveAlert(Guid id, CancellationToken ct)
    {
        var result = await _weightLogService.ResolveAlertAsync(id, GetCurrentUserId(), ct);
        return Success(result, "Alert resolved successfully");
    }
}
