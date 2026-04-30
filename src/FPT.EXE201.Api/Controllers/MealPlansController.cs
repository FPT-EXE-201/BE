using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

/// <summary>
/// Exception: Controller này inject 2 services (MealPlanService + FeedbackService)
/// vì plan feedback là sub-resource của meal plan, tách controller riêng không hợp lý.
/// </summary>
[Route("api")]
[Authorize]
public class MealPlansController : BaseApiController
{
    private readonly IMealPlanService _mealPlanService;
    private readonly INutritionFeedbackService _feedbackService;

    public MealPlansController(
        IMealPlanService mealPlanService,
        INutritionFeedbackService feedbackService)
    {
        _mealPlanService = mealPlanService;
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Queue AI meal plan generation for a single day. Returns 202 Accepted.
    /// Poll GET /api/meal-plans/{id}/status for progress.
    /// Rate limited: 15 AI calls/day.
    /// </summary>
    [HttpPost("pregnancies/{pregnancyId:guid}/meal-plans/generate")]
    [RequirePermission("meal_plan.generate")]
    public async Task<IActionResult> Generate(
        Guid pregnancyId, [FromBody] GenerateMealPlanDto dto, CancellationToken ct = default)
    {
        var result = await _mealPlanService.GenerateAsync(
            pregnancyId, GetCurrentUserId(), dto, ct);
        return Accepted(result, "Daily meal plan generation queued. Poll /status for progress.");
    }

    /// <summary>
    /// Poll meal plan generation status. FE calls every 3-5s until Succeeded/Failed.
    /// </summary>
    [HttpGet("meal-plans/{planId:guid}/status")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetStatus(
        Guid planId, CancellationToken ct = default)
    {
        var result = await _mealPlanService.GetStatusAsync(
            planId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/meal-plans")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> List(
        Guid pregnancyId, [FromQuery] QueryOptions options, CancellationToken ct = default)
    {
        var result = await _mealPlanService.ListAsync(
            pregnancyId, GetCurrentUserId(), options, ct);
        return Success(result);
    }

    [HttpGet("pregnancies/{pregnancyId:guid}/meal-plans/{planId:guid}")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetDetail(
        Guid pregnancyId, Guid planId, CancellationToken ct = default)
    {
        var result = await _mealPlanService.GetDetailAsync(
            planId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/meal-plans/{planId:guid}")]
    [RequirePermission("meal_plan.delete")]
    public async Task<IActionResult> Delete(
        Guid pregnancyId, Guid planId, CancellationToken ct = default)
    {
        await _mealPlanService.DeleteAsync(planId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Meal plan deleted successfully");
    }

    /// <summary>
    /// Get meal plan detail for a specific date.
    /// </summary>
    [HttpGet("meal-plans/{planId:guid}/days/{date}")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetDayDetail(
        Guid planId, DateOnly date,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _mealPlanService.GetDayDetailAsync(
            planId, date, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Get daily meal detail by pregnancy/date without requiring FE to know planId.
    /// </summary>
    [HttpGet("pregnancies/{pregnancyId:guid}/meal-days/{date}")]
    [RequirePermission("meal_plan.read")]
    public async Task<IActionResult> GetDayByPregnancyDate(
        Guid pregnancyId, DateOnly date,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _mealPlanService.GetDayByPregnancyDateAsync(
            pregnancyId, date, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    /// <summary>
    /// Rate overall meal plan (1-5 stars). One feedback per user per plan.
    /// </summary>
    [HttpPost("meal-plans/{planId:guid}/feedback")]
    [RequirePermission("meal_plan_feedback.write")]
    public async Task<IActionResult> CreatePlanFeedback(
        Guid planId, [FromBody] CreateMealPlanFeedbackDto dto, CancellationToken ct = default)
    {
        var result = await _feedbackService.CreatePlanFeedbackAsync(
            planId, GetCurrentUserId(), dto, ct);
        return Created(result, "Feedback submitted successfully");
    }
}
