using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api/meal-items")]
[Authorize]
public class MealItemsController : BaseApiController
{
    private readonly INutritionFeedbackService _feedbackService;

    public MealItemsController(INutritionFeedbackService feedbackService)
    {
        _feedbackService = feedbackService;
    }

    /// <summary>
    /// Like/dislike a meal item. One feedback per user per item.
    /// </summary>
    [HttpPost("{itemId:guid}/feedback")]
    [RequirePermission("meal_item_feedback.write")]
    public async Task<IActionResult> CreateItemFeedback(
        Guid itemId, [FromBody] CreateMealItemFeedbackDto dto, CancellationToken ct = default)
    {
        var result = await _feedbackService.CreateItemFeedbackAsync(
            itemId, GetCurrentUserId(), dto, ct);
        return Created(result, "Feedback submitted successfully");
    }
}
