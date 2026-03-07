using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanFeedbackRepository
    : GenericRepository<MealPlanFeedback>, IMealPlanFeedbackRepository
{
    public MealPlanFeedbackRepository(AppDbContext context) : base(context) { }

    public async Task<MealPlanFeedback?> FindByKeyIncludingDeletedAsync(
        Guid mealPlanId, Guid userId, CancellationToken ct = default)
    {
        // IgnoreQueryFilters to also find soft-deleted entries (DB unique constraint includes them)
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(
                f => f.MealPlanId == mealPlanId && f.UserId == userId, ct);
    }
}
