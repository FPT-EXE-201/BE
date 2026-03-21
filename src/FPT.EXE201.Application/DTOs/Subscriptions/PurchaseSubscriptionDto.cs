namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Request DTO khi user mua subscription.
/// </summary>
public record PurchaseSubscriptionDto(
    /// <summary>Gói: "Monthly", "SixMonths", "Yearly"</summary>
    string Plan
);
