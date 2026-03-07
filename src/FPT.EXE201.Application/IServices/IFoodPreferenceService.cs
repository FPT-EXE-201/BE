using FPT.EXE201.Application.DTOs.Nutrition;

namespace FPT.EXE201.Application.IServices;

public interface IFoodPreferenceService
{
    // Food Preferences
    Task<List<FoodPreferenceDto>> GetPreferencesAsync(
        Guid pregnancyId, Guid userId, string langCode = "vi",
        CancellationToken ct = default);
    Task<FoodPreferenceDto> CreatePreferenceAsync(
        Guid pregnancyId, Guid userId, CreateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default);
    Task<FoodPreferenceDto> UpdatePreferenceAsync(
        Guid prefId, Guid userId, UpdateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default);
    Task DeletePreferenceAsync(
        Guid prefId, Guid userId, CancellationToken ct = default);

    // Nutrition Notes
    Task<List<NutritionNoteDto>> GetNotesAsync(
        Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<NutritionNoteDto> CreateNoteAsync(
        Guid pregnancyId, Guid userId, CreateNutritionNoteDto dto,
        CancellationToken ct = default);
    Task<NutritionNoteDto> UpdateNoteAsync(
        Guid noteId, Guid userId, UpdateNutritionNoteDto dto,
        CancellationToken ct = default);
    Task DeleteNoteAsync(
        Guid noteId, Guid userId, CancellationToken ct = default);
}
