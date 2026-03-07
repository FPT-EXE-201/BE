namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Reference nutrient catalog (15 items: PROTEIN, IRON, CALCIUM...).
/// ⚠️ Decision #2: Custom entity — KHÔNG kế thừa BaseEntity, KHÔNG có soft delete.
/// Has created_at + updated_at only (no deleted_at).
/// </summary>
public class RefNutrient
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Code { get; set; } = string.Empty;
    public string Unit { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;

    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }

    // Navigation
    public ICollection<RefNutrientTranslation> Translations { get; set; }
        = new List<RefNutrientTranslation>();
    public ICollection<MealItemNutrient> MealItemNutrients { get; set; }
        = new List<MealItemNutrient>();
}
