using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IRepositories;

public interface IPregnancyFoodPreferenceRepository : IGenericRepository<PregnancyFoodPreference>
{
    Task<List<PregnancyFoodPreference>> GetByPregnancyIdAsync(
        Guid pregnancyId, string langCode, CancellationToken ct = default);
    /// <summary>
    /// Find by unique key including soft-deleted records (IgnoreQueryFilters)
    /// to handle DB unique constraint with soft delete.
    /// </summary>
    Task<PregnancyFoodPreference?> FindByKeyIncludingDeletedAsync(
        Guid pregnancyId, Guid foodItemId, FoodPreferenceType type,
        CancellationToken ct = default);
}
