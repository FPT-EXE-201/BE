using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// A single food item in a meal (Breakfast/Lunch/Dinner/Snack).
/// Links to a Recipe for instructions.
/// </summary>
public class MealItem : BaseEntity
{
    public Guid MealDayId { get; set; }
    public MealType MealType { get; set; }
    public Guid? RecipeId { get; set; }
    public string? ItemName { get; set; }
    public string? PortionText { get; set; }
    public int? CaloriesKcal { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public MealPlanDay MealDay { get; set; } = null!;
    public Recipe? Recipe { get; set; }
    public ICollection<MealItemNutrient> Nutrients { get; set; } = new List<MealItemNutrient>();
    public ICollection<MealItemFeedback> Feedbacks { get; set; }
        = new List<MealItemFeedback>();
}
