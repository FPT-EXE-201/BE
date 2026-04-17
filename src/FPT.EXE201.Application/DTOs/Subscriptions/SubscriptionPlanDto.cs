namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Response: thông tin gói subscription (cho FE hiển thị trang pricing).
/// </summary>
public class SubscriptionPlanDto
{
    public string Plan { get; set; } = null!;
    public string Name { get; set; } = null!;
    public decimal Price { get; set; }
    /// <summary>
    /// ProductId tương ứng trên Apple App Store (StoreKit) để app dùng mua qua StoreKit.
    /// </summary>
    public string AppleProductId { get; set; } = null!;
    public int DurationMonths { get; set; }
    public decimal PricePerMonth { get; set; }
    public int? SavePercent { get; set; }
}
