using FPT.EXE201.Application.IRepositories;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;
using FPT.EXE201.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace FPT.EXE201.Infrastructure.Repositories;

public class SubscriptionRepository : GenericRepository<Subscription>, ISubscriptionRepository
{
    public SubscriptionRepository(AppDbContext context) : base(context) { }

    public async Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Active && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<Subscription?> GetByOrderCodeAsync(long orderCode, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.OrderCode == orderCode && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Subscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.Status == SubscriptionStatus.Active
                        && s.EndDate < DateTime.UtcNow
                        && s.DeletedAt == null)
            .ToListAsync(ct);
    }

    public async Task<List<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.DeletedAt == null)
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }

    public async Task<Subscription?> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.UserId == userId && s.Status == SubscriptionStatus.Pending && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
    }
}
