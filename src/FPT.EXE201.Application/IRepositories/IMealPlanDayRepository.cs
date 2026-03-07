using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanDayRepository : IGenericRepository<MealPlanDay>
{
    Task<MealPlanDay?> GetByPlanIdAndDateAsync(
        Guid planId, DateOnly date, CancellationToken ct = default);
}
