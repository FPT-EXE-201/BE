namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// i18n translation for RefFoodItem.
/// ⚠️ KHÔNG kế thừa BaseEntity — composite primary key.
/// </summary>
public class RefFoodItemTranslation
{
    public Guid FoodItemId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Navigation
    public RefFoodItem FoodItem { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
