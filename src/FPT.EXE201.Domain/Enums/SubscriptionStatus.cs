namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Trạng thái subscription.
/// </summary>
public enum SubscriptionStatus
{
    /// <summary>Đang chờ thanh toán</summary>
    Pending,

    /// <summary>Đang hoạt động (đã thanh toán)</summary>
    Active,

    /// <summary>Hết hạn (background job set)</summary>
    Expired,

    /// <summary>User hủy</summary>
    Cancelled,

    /// <summary>Apple billing thất bại, đang trong grace period (vẫn còn quyền tạm thời)</summary>
    GracePeriod,

    /// <summary>Apple đang thử charge lại sau khi billing thất bại</summary>
    BillingRetry
}
