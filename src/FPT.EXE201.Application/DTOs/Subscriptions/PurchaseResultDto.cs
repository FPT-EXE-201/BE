namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Response khi tạo payment: chứa checkoutUrl để FE redirect.
/// </summary>
public class PurchaseResultDto
{
    public Guid SubscriptionId { get; set; }
    public long OrderCode { get; set; }
    public string CheckoutUrl { get; set; } = null!;
}
