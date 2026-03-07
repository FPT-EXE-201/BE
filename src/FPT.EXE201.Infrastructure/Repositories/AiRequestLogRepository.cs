using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class AiRequestLogRepository
    : GenericRepository<AiRequestLog>, IAiRequestLogRepository
{
    public AiRequestLogRepository(AppDbContext context) : base(context) { }

    public async Task<int> CountTodayByUserAsync(Guid userId, CancellationToken ct = default)
    {
        var todayUtc = DateTime.UtcNow.Date;
        return await _dbSet.CountAsync(
            l => l.UserId == userId
                 && l.Feature == AiFeature.NutritionMealPlan
                 && l.CreatedAt >= todayUtc, ct);
    }
}
