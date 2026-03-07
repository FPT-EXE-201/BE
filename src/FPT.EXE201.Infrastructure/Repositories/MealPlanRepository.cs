using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.MealPlans;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class MealPlanRepository : GenericRepository<MealPlan>, IMealPlanRepository
{
    public MealPlanRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<MealPlan>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default)
    {
        return await GetPagedAsync(
            options,
            predicate: m => m.PregnancyId == pregnancyId,
            include: q => q.Include(m => m.Days),
            searchBuilder: SearchHelper.CreateSearchBuilder(
                MealPlanListQuerySpec.SearchMap,
                MealPlanListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: MealPlanListQuerySpec.SortMap,
            defaultSort: MealPlanListQuerySpec.DefaultSort,
            cancellationToken: ct);
    }

    public async Task<MealPlan?> GetByIdWithDetailsAsync(Guid id, CancellationToken ct = default)
    {
        return await _dbSet
            .Include(m => m.Days.OrderBy(d => d.PlanDate))
                .ThenInclude(d => d.Items.OrderBy(i => i.MealType))
                    .ThenInclude(i => i.Nutrients)
                        .ThenInclude(n => n.Nutrient)
                            .ThenInclude(rn => rn.Translations)
            .Include(m => m.Days)
                .ThenInclude(d => d.Items)
                    .ThenInclude(i => i.Recipe)
            .FirstOrDefaultAsync(m => m.Id == id, ct);
    }

    public async Task<List<MealPlan>> GetOverlappingAsync(
        Guid pregnancyId, DateOnly startDate, DateOnly endDate,
        CancellationToken ct = default)
    {
        return await _dbSet
            .Where(m => m.PregnancyId == pregnancyId
                        && m.StartDate <= endDate
                        && m.EndDate >= startDate)
            .ToListAsync(ct);
    }
}
