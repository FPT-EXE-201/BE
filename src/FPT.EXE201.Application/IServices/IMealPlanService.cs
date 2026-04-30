using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IMealPlanService
{
    /// <summary>
    /// Queue meal plan for AI generation. Returns immediately with Pending status.
    /// </summary>
    Task<MealPlanStatusDto> GenerateAsync(
        Guid pregnancyId, Guid userId, GenerateMealPlanDto dto,
        CancellationToken ct = default);

    /// <summary>
    /// Process meal plan generation in background. Called by MealPlanBackgroundService.
    /// </summary>
    Task ProcessGenerationAsync(MealPlanJobItem job, CancellationToken ct = default);

    /// <summary>
    /// Get current generation status (for polling).
    /// </summary>
    Task<MealPlanStatusDto> GetStatusAsync(
        Guid planId, Guid userId, CancellationToken ct = default);

    Task<PagedResult<MealPlanSummaryDto>> ListAsync(
        Guid pregnancyId, Guid userId, QueryOptions options,
        CancellationToken ct = default);
    Task<MealPlanDetailDto> GetDetailAsync(
        Guid planId, Guid userId, CancellationToken ct = default);
    Task DeleteAsync(
        Guid planId, Guid userId, CancellationToken ct = default);
    Task<MealDayDetailDto> GetDayDetailAsync(
        Guid planId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default);
    Task<MealDayDetailDto> GetDayByPregnancyDateAsync(
        Guid pregnancyId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default);
}
