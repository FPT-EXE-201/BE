using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanDayRepository : GenericRepository<MealPlanDay>, IMealPlanDayRepository
{
    public MealPlanDayRepository(AppDbContext context) : base(context) { }

    public async Task<MealPlanDay?> GetByPlanIdAndDateAsync(
        Guid planId, DateOnly date, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(d => d.MealPlanId == planId && d.PlanDate == date)
            .Include(d => d.Items.OrderBy(i => i.MealType))
                .ThenInclude(i => i.Nutrients)
                    .ThenInclude(n => n.Nutrient)
                        .ThenInclude(rn => rn.Translations)
            .Include(d => d.Items)
                .ThenInclude(i => i.Recipe)
            .FirstOrDefaultAsync(ct);
    }
}
