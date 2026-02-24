using FPT.EXE201.Application.Common.Querying;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.WeightLogs;
using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class WeightLogRepository : GenericRepository<WeightLog>, IWeightLogRepository
{
    public WeightLogRepository(AppDbContext context) : base(context) { }

    public async Task<PagedResult<WeightLog>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, QueryOptions options, CancellationToken ct = default)
    {
        return await GetPagedAsync(
            options,
            predicate: w => w.PregnancyId == pregnancyId,
            include: null,
            searchBuilder: SearchHelper.CreateSearchBuilder(
                WeightLogListQuerySpec.SearchMap,
                WeightLogListQuerySpec.DefaultSearchKeys,
                options),
            sortMap: WeightLogListQuerySpec.SortMap,
            defaultSort: WeightLogListQuerySpec.DefaultSort,
            cancellationToken: ct);
    }

    public async Task<List<WeightLog>> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderBy(w => w.LoggedOn)
            .ToListAsync(ct);
    }

    public async Task<WeightLog?> GetByPregnancyAndDateAsync(
        Guid pregnancyId, DateOnly loggedOn, CancellationToken ct = default)
    {
        // IgnoreQueryFilters to also find soft-deleted entries (DB unique constraint includes them)
        return await _dbSet
            .IgnoreQueryFilters()
            .FirstOrDefaultAsync(w => w.PregnancyId == pregnancyId && w.LoggedOn == loggedOn, ct);
    }

    public async Task<WeightLog?> GetLatestByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderByDescending(w => w.LoggedOn)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<WeightLog>> GetRecentByPregnancyIdAsync(
        Guid pregnancyId, int count = 5, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(w => w.PregnancyId == pregnancyId)
            .OrderByDescending(w => w.LoggedOn)
            .Take(count)
            .ToListAsync(ct);
    }
}
