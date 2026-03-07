namespace FPT.EXE201.Domain.Enums;

/// <summary>
/// Status of a meal plan generation request.
/// Flow: Pending → Generating → Succeeded/Failed
/// </summary>
public enum MealPlanStatus
{
    Pending,       // Queued, waiting for background worker
    Generating,    // AI is generating meal plan (week by week)
    Succeeded,     // All weeks generated successfully
    Failed         // Generation failed (partial or total)
}
