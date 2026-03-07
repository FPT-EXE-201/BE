using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User feedback on a specific MealItem (liked + optional comment).
/// Decision #1: Inherits BaseEntity.
/// </summary>
public class MealItemFeedback : BaseEntity
{
    public Guid MealItemId { get; set; }
    public Guid UserId { get; set; }
    public bool Liked { get; set; }
    public string? Comment { get; set; }

    // Navigation
    public MealItem MealItem { get; set; } = null!;
    public User User { get; set; } = null!;
}
