using FPT.EXE201.Application.DTOs.Subscriptions;

namespace FPT.EXE201.Application.IServices;

public interface ISubscriptionService
{
    /// <summary>Tạo subscription + payment link PayOS.</summary>
    Task<PurchaseResultDto> PurchaseAsync(Guid userId, PurchaseSubscriptionDto dto, CancellationToken ct = default);

    /// <summary>Xử lý PayOS webhook callback.</summary>
    Task HandlePaymentWebhookAsync(long orderCode, string? transactionId, bool isSuccess, CancellationToken ct = default);

    /// <summary>Lấy trạng thái subscription hiện tại.</summary>
    Task<SubscriptionStatusDto> GetStatusAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lấy lịch sử subscription.</summary>
    Task<List<SubscriptionHistoryDto>> GetHistoryAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Hủy subscription đang active.</summary>
    Task CancelAsync(Guid userId, CancellationToken ct = default);

    /// <summary>Lấy danh sách gói (pricing page).</summary>
    List<SubscriptionPlanDto> GetPlans();

    /// <summary>Background job: expire hết hạn + remove PREMIUM role.</summary>
    Task CheckExpiredSubscriptionsAsync(CancellationToken ct = default);

    /// <summary>Verify payment status từ PayOS (dùng khi user quay về app sau checkout).</summary>
    Task<SubscriptionStatusDto> VerifyAndActivateAsync(Guid userId, long orderCode, CancellationToken ct = default);
}
