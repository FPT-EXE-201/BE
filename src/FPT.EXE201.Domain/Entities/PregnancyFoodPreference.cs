using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// User allergens/dislikes per pregnancy. FK → ref_food_items.
/// Unique constraint: (pregnancy_id, food_item_id, preference_type).
/// </summary>
public class PregnancyFoodPreference : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public Guid FoodItemId { get; set; }
    public FoodPreferenceType PreferenceType { get; set; }
    public AllergySeverity? Severity { get; set; }
    public string? Notes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public RefFoodItem FoodItem { get; set; } = null!;
}
