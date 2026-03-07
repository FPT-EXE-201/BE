using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface IMealItemFeedbackRepository : IGenericRepository<MealItemFeedback>
{
    /// <summary>
    /// Find by unique key including soft-deleted records (IgnoreQueryFilters)
    /// to handle DB unique constraint with soft delete.
    /// </summary>
    Task<MealItemFeedback?> FindByKeyIncludingDeletedAsync(
        Guid mealItemId, Guid userId, CancellationToken ct = default);
}
