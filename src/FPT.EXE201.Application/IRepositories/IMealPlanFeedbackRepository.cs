using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealPlanFeedbackRepository : IGenericRepository<MealPlanFeedback>
{
    /// <summary>
    /// Find by unique key including soft-deleted records (IgnoreQueryFilters)
    /// to handle DB unique constraint with soft delete.
    /// </summary>
    Task<MealPlanFeedback?> FindByKeyIncludingDeletedAsync(
        Guid mealPlanId, Guid userId, CancellationToken ct = default);
}
