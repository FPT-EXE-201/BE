namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// i18n translation for RefNutrient.
/// ⚠️ KHÔNG kế thừa BaseEntity — composite primary key.
/// </summary>
public class RefNutrientTranslation
{
    public Guid NutrientId { get; set; }
    public string LanguageCode { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;

    // Navigation
    public RefNutrient Nutrient { get; set; } = null!;
    public Language Language { get; set; } = null!;
}
