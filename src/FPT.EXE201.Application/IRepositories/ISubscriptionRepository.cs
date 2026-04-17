using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

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

    /// <summary>Lấy subscription theo Apple originalTransactionId (dùng cho idempotency check và notification handling).</summary>
    Task<Subscription?> GetByAppleOriginalTransactionIdAsync(string originalTransactionId, CancellationToken ct = default);

    /// <summary>Lấy tất cả các giao dịch kèm thông tin User & Profile (dùng cho Admin, hỗ trợ lọc).</summary>
    Task<List<Subscription>> GetAllWithUserAndProfileAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SubscriptionStatus? status = null,
        CancellationToken ct = default);

    // ── Stats helpers ──

    /// <summary>Đếm số userId có subscription Active và EndDate > now (Còn hạn Pro).</summary>
    Task<int> CountActiveProUsersAsync(CancellationToken ct = default);

    /// <summary>Đếm số userId có subscription Expired nhưng KHÔNG có Active hiện tại (Hết hạn Pro).</summary>
    Task<int> CountExpiredProUsersAsync(CancellationToken ct = default);

    /// <summary>
    /// Lấy subscription Active cho một danh sách userId (dùng để hiển thị gói trong bảng user list).
    /// Trả về tối đa 1 subscription Active mỗi user.
    /// </summary>
    Task<List<Subscription>> GetActiveSubscriptionsByUserIdsAsync(List<Guid> userIds, CancellationToken ct = default);
}
