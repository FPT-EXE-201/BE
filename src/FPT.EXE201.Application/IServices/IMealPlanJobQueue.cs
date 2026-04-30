namespace FPT.EXE201.Application.IServices;

/// <summary>
/// In-process job queue for AI meal plan generation.
/// Uses System.Threading.Channels internally — no external dependencies.
/// Same pattern as IOcrJobQueue (Week 5).
/// </summary>
public interface IMealPlanJobQueue
{
    /// <summary>Enqueue a meal plan for AI generation. Returns immediately.</summary>
    ValueTask EnqueueAsync(MealPlanJobItem job, CancellationToken cancellationToken = default);

    /// <summary>Dequeue next job (blocks until available). Used by BackgroundService.</summary>
    ValueTask<MealPlanJobItem> DequeueAsync(CancellationToken cancellationToken);
}

/// <summary>Job payload for background meal plan generation.</summary>
public record MealPlanJobItem(
    Guid MealPlanId,
    Guid PregnancyId,
    Guid UserId,
    DateOnly PlanDate,
    string? AdditionalNotes = null,
    IReadOnlyList<Guid>? ReplacedMealPlanIds = null
);
