using FPT.EXE201.Domain.Common;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Domain.Entities;

/// <summary>
/// Free-text dietary notes per pregnancy (e.g., "thích món miền Tây").
/// Supplements structured food preferences with flexible text notes.
/// </summary>
public class PregnancyNutritionNote : BaseEntity
{
    public Guid PregnancyId { get; set; }
    public NutritionNoteType NoteType { get; set; } = NutritionNoteType.Note;
    public string ValueText { get; set; } = string.Empty;

    // Navigation
    public Pregnancy Pregnancy { get; set; } = null!;
}
