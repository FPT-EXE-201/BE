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

    public async Task<Subscription?> GetByAppleOriginalTransactionIdAsync(string originalTransactionId, CancellationToken ct = default)
    {
        return await _dbSet
            .Where(s => s.AppleOriginalTransactionId == originalTransactionId && s.DeletedAt == null)
            .FirstOrDefaultAsync(ct);
    }

    public async Task<List<Subscription>> GetAllWithUserAndProfileAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SubscriptionStatus? status = null,
        CancellationToken ct = default)
    {
        var query = _dbSet
            .Include(s => s.User)
            .ThenInclude(u => u.Profile)
            .Where(s => s.DeletedAt == null);

        // Exclude test/sandbox transactions (reference starting with TEST_ or SANDBOX_)
        query = query.Where(s => 
            (string.IsNullOrEmpty(s.AppleOriginalTransactionId) || (!s.AppleOriginalTransactionId.StartsWith("TEST_") && !s.AppleOriginalTransactionId.StartsWith("SANDBOX_"))) &&
            (string.IsNullOrEmpty(s.PaymentTransactionId) || (!s.PaymentTransactionId.StartsWith("TEST_") && !s.PaymentTransactionId.StartsWith("SANDBOX_")))
        );

        if (startDate.HasValue)
            query = query.Where(s => s.CreatedAt >= startDate.Value);

        if (endDate.HasValue)
            query = query.Where(s => s.CreatedAt <= endDate.Value);

        if (status.HasValue)
            query = query.Where(s => s.Status == status.Value);

        return await query
            .OrderByDescending(s => s.CreatedAt)
            .ToListAsync(ct);
    }
}
