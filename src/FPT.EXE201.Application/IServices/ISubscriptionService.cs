using FPT.EXE201.Application.DTOs.Subscriptions;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.IServices;

public interface ISubscriptionService
{
    /// <summary>Tạo subscription + payment link PayOS (tự động chọn Web/App).</summary>
    Task<PurchaseResultDto> PurchaseAsync(Guid userId, PurchaseSubscriptionDto dto, bool isWeb = false, CancellationToken ct = default);

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

    /// <summary>Đăng ký Webhook URL từ cấu hình lên PayOS Dashboard qua SDK.</summary>
    Task<bool> RegisterWebhookAsync(CancellationToken ct = default);

    /// <summary>Verify payment status từ PayOS (dùng khi user quay về app sau checkout).</summary>
    Task<SubscriptionStatusDto> VerifyAndActivateAsync(Guid userId, long orderCode, CancellationToken ct = default);

    /// <summary>Verify signedTransactionInfo JWS từ StoreKit 2, tạo subscription Active và cấp quyền PREMIUM.</summary>
    Task<SubscriptionStatusDto> VerifyAppleIapAsync(Guid userId, AppleIapVerifyDto dto, CancellationToken ct = default);

    /// <summary>Xử lý App Store Server Notification V2 (DID_RENEW, EXPIRED, REFUND, GRACE_PERIOD...).</summary>
    Task HandleAppleNotificationAsync(string notificationType, string subtype,
        string originalTransactionId, string productId, long? expiresDateMs, CancellationToken ct = default);

    /// <summary>
    /// [SANDBOX ONLY] Kích hoạt Apple IAP subscription trực tiếp không cần JWS.
    /// Chỉ hoạt động khi AppStore:Environment = Sandbox. Dùng để test backend.
    /// </summary>
    Task<SubscriptionStatusDto> ActivateAppleIapSandboxAsync(Guid userId, string plan, string fakeTransactionId, CancellationToken ct = default);

    /// <summary>Lấy tất cả các giao dịch kèm thông tin User (dành cho Admin, hỗ trợ lọc).</summary>
    Task<List<TransactionAdminDto>> GetAllTransactionsForAdminAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SubscriptionStatus? status = null, 
        CancellationToken ct = default);

    /// <summary>Xuất danh sách giao dịch ra file PDF (hỗ trợ lọc).</summary>
    Task<byte[]> ExportTransactionsPdfAsync(
        DateTime? startDate = null, 
        DateTime? endDate = null, 
        SubscriptionStatus? status = null, 
        CancellationToken ct = default);
}
