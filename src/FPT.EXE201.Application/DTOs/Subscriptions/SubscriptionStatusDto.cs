namespace FPT.EXE201.Application.DTOs.Subscriptions;

/// <summary>
/// Response: trạng thái subscription hiện tại của user.
/// </summary>
public class SubscriptionStatusDto
{
    public bool IsPremium { get; set; }
    public string? Plan { get; set; }
    public decimal? Price { get; set; }
    public DateTime? StartDate { get; set; }
    public DateTime? EndDate { get; set; }
    public int? DaysRemaining { get; set; }
    public bool IsExpiringSoon { get; set; }
}
