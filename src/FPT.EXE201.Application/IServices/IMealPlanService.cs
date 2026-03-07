using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IMealPlanService
{
    Task<MealPlanDetailDto> GenerateAsync(
        Guid pregnancyId, Guid userId, GenerateMealPlanDto dto,
        CancellationToken ct = default);
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
}
