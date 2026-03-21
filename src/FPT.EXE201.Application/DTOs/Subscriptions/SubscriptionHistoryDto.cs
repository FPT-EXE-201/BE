namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Response: 1 record trong lịch sử subscription.
/// </summary>
public class SubscriptionHistoryDto
{
    public Guid Id { get; set; }
    public string Plan { get; set; } = null!;
    public decimal Price { get; set; }
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public string Status { get; set; } = null!;
    public long OrderCode { get; set; }
    public DateTime CreatedAt { get; set; }
}
