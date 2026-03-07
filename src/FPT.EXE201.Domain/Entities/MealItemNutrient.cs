namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Bridge table: meal_item ↔ nutrient with amount.
/// ⚠️ KHÔNG kế thừa BaseEntity — composite primary key.
/// </summary>
public class MealItemNutrient
{
    public Guid MealItemId { get; set; }
    public Guid NutrientId { get; set; }
    public decimal Amount { get; set; }

    // Navigation
    public MealItem MealItem { get; set; } = null!;
    public RefNutrient Nutrient { get; set; } = null!;
}
