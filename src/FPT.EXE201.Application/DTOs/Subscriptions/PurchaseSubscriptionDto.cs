namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Request DTO khi user mua subscription.
/// </summary>
public record PurchaseSubscriptionDto(
    /// <summary>Gói: "Monthly", "SixMonths", "Yearly"</summary>
    string Plan,
    /// <summary>URL quay về sau khi thanh toán thành công (Tùy chọn)</summary>
    string? ReturnUrl = null,
    /// <summary>URL quay về sau khi hủy thanh toán (Tùy chọn)</summary>
    string? CancelUrl = null
);
