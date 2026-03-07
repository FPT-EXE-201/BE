using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Reference food item catalog (~60-80 items).
/// Used for preference/allergy picker UI only — NOT an ingredient database.
/// </summary>
public class RefFoodItem : BaseEntity
{
    public string Code { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    // Navigation
    public ICollection<RefFoodItemTranslation> Translations { get; set; }
        = new List<RefFoodItemTranslation>();
    public ICollection<PregnancyFoodPreference> FoodPreferences { get; set; }
        = new List<PregnancyFoodPreference>();
}
