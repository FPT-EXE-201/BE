using FPT.EXE201.Domain.Common;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// AI-generated recipe, pregnancy-scoped.
/// 1 recipe per meal item (REQUIRED — Decision #8).
/// </summary>
public class Recipe : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string? Instructions { get; set; }
    public int? Servings { get; set; }
    public int? PrepMinutes { get; set; }
    public int? CookMinutes { get; set; }

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
    public ICollection<MealItem> MealItems { get; set; } = new List<MealItem>();
}
