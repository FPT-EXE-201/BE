using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class PregnancyFoodPreferenceRepository
    : GenericRepository<PregnancyFoodPreference>, IPregnancyFoodPreferenceRepository
{
    public PregnancyFoodPreferenceRepository(AppDbContext context) : base(context) { }

    public async Task<List<PregnancyFoodPreference>> GetByPregnancyIdAsync(
        Guid pregnancyId, string langCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(p => p.PregnancyId == pregnancyId)
            .Include(p => p.FoodItem)
                .ThenInclude(fi => fi.Translations.Where(t => t.LanguageCode == langCode))
            .OrderBy(p => p.PreferenceType)
            .ThenBy(p => p.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<PregnancyFoodPreference?> FindByKeyIncludingDeletedAsync(
        Guid pregnancyId, Guid foodItemId, FoodPreferenceType type,
        CancellationToken ct = default)
    {
        // IgnoreQueryFilters to also find soft-deleted entries (DB unique constraint includes them)
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                p => p.PregnancyId == pregnancyId
                     && p.FoodItemId == foodItemId
                     && p.PreferenceType == type, ct);
    }
}
