using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// A weekly meal plan for a pregnancy.
/// May be AI-generated or manually created.
/// </summary>
public class MealPlan : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public Guid? AiRequestLogId { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly EndDate { get; set; }
    public MealPlanSource Source { get; set; } = MealPlanSource.AI;
    public MealPlanStatus Status { get; set; } = MealPlanStatus.Pending;
    public int CompletedWeeks { get; set; }
    public int TotalWeeks { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Title { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public AiRequestLog? AiRequestLog { get; set; }
    public ICollection<MealPlanDay> Days { get; set; } = new List<MealPlanDay>();
    public ICollection<MealPlanFeedback> Feedbacks { get; set; } = new List<MealPlanFeedback>();
}
