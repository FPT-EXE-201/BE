using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Subscription premium của user.
/// Mỗi user chỉ có tối đa 1 subscription ACTIVE tại 1 thời điểm.
/// Khi mua thành công → gán role PREMIUM → JWT có thêm premium permissions.
/// Khi hết hạn / hủy → xóa role PREMIUM.
/// </summary>
public class Subscription : BaseEntity
{
    /// <summary>ID user sở hữu subscription.</summary>
    public Guid UserId { get; set; }

    /// <summary>Gói: Monthly / SixMonths / Yearly.</summary>
    public SubscriptionPlan Plan { get; set; }

    /// <summary>Giá gốc tại thời điểm mua (VND).</summary>
    public decimal Price { get; set; }

    /// <summary>Ngày bắt đầu subscription.</summary>
    public DateTime StartDate { get; set; }

    /// <summary>Ngày hết hạn subscription.</summary>
    public DateTime EndDate { get; set; }

    /// <summary>Trạng thái: Pending → Active → Expired / Cancelled.</summary>
    public SubscriptionStatus Status { get; set; } = SubscriptionStatus.Pending;

    /// <summary>Mã đơn hàng PayOS (orderCode).</summary>
    public long OrderCode { get; set; }

    /// <summary>Mã giao dịch từ PayOS (sau khi thanh toán thành công).</summary>
    public string? PaymentTransactionId { get; set; }

    /// <summary>Apple originalTransactionId — idempotency key khi verify và renewal từ StoreKit 2.</summary>
    public string? AppleOriginalTransactionId { get; set; }

    /// <summary>Apple productId — com.pregtap.subscription.monthly/sixmonths/yearly.</summary>
    public string? AppleProductId { get; set; }

    // ── Navigation ──
    public User User { get; set; } = null!;
}
