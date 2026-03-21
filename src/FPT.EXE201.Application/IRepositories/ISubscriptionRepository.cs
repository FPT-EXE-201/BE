using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

public interface ISubscriptionRepository : IGenericRepository<Subscription>
{
    /// <summary>Lấy subscription Active của user (tối đa 1).</summary>
    Task<Subscription?> GetActiveByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lấy subscription theo orderCode PayOS.</summary>
    Task<Subscription?> GetByOrderCodeAsync(long orderCode, CancellationToken ct = default);

    /// <summary>Lấy danh sách subscription đã hết hạn nhưng vẫn Active (cần expire).</summary>
    Task<List<Subscription>> GetExpiredActiveSubscriptionsAsync(CancellationToken ct = default);

    /// <summary>Lấy lịch sử subscription của user (mới nhất trước).</summary>
    Task<List<Subscription>> GetByUserIdAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lấy subscription Pending của user (chưa thanh toán).</summary>
    Task<Subscription?> GetPendingByUserIdAsync(Guid userId, CancellationToken ct = default);
}
