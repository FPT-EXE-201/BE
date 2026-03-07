using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanRepository : IGenericRepository<MealPlan>
{
    Task<PagedResult<MealPlan>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default);
    Task<MealPlan?> GetByIdWithDetailsAsync(
        Guid id, CancellationToken ct = default);
    Task<List<MealPlan>> GetOverlappingAsync(
        Guid pregnancyId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default);
}
