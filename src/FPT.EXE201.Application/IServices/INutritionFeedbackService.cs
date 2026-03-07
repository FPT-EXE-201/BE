using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface INutritionFeedbackService
{
    Task<MealPlanFeedbackDto> CreatePlanFeedbackAsync(
        Guid planId, Guid userId, CreateMealPlanFeedbackDto dto,
        CancellationToken ct = default);
    Task<MealItemFeedbackDto> CreateItemFeedbackAsync(
        Guid itemId, Guid userId, CreateMealItemFeedbackDto dto,
        CancellationToken ct = default);
}
