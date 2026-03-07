using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// One day inside a MealPlan.
/// Decision #1: Inherits BaseEntity (has Id, timestamps, soft delete).
/// </summary>
public class MealPlanDay : BaseEntity
{
    public Guid MealPlanId { get; set; }
    public DateOnly PlanDate { get; set; }

    // Navigation
    public MealPlan MealPlan { get; set; } = null!;
    public ICollection<MealItem> Items { get; set; } = new List<MealItem>();
}
