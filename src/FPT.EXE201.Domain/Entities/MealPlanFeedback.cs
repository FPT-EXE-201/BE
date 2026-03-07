using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User feedback for an entire MealPlan (1–5 stars + comment).
/// Decision #1: Inherits BaseEntity.
/// </summary>
public class MealPlanFeedback : BaseEntity
{
    public Guid MealPlanId { get; set; }
    public Guid UserId { get; set; }
    public int Rating { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public MealPlan MealPlan { get; set; } = null!;
    public User User { get; set; } = null!;
}
