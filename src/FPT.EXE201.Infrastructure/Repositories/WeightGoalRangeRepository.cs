using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class WeightGoalRangeRepository : GenericRepository<WeightGoalRange>, IWeightGoalRangeRepository
{
    public WeightGoalRangeRepository(AppDbContext context) : base(context) { }

    public async Task<WeightGoalRange?> GetByPregnancyIdAsync(
        Guid pregnancyId, CancellationToken ct = default)
    {
        return await _dbSet
            .FirstOrDefaultAsync(g => g.PregnancyId == pregnancyId, ct);
    }
}
