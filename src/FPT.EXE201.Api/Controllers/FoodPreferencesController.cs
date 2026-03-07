using FPT.EXE201.Application.Authorization;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace FPT.EXE201.Api.Controllers;

[Route("api")]
[Authorize]
public class FoodPreferencesController : BaseApiController
{
    private readonly IFoodPreferenceService _service;

    public FoodPreferencesController(IFoodPreferenceService service)
    {
        _service = service;
    }

    // ═══ Food Preferences ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/food-preferences")]
    [RequirePermission("food_preference.read")]
    public async Task<IActionResult> GetPreferences(
        Guid pregnancyId, [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.GetPreferencesAsync(
            pregnancyId, GetCurrentUserId(), lang, ct);
        return Success(result);
    }

    [HttpPost("pregnancies/{pregnancyId:guid}/food-preferences")]
    [RequirePermission("food_preference.write")]
    public async Task<IActionResult> CreatePreference(
        Guid pregnancyId, [FromBody] CreateFoodPreferenceDto dto,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.CreatePreferenceAsync(
            pregnancyId, GetCurrentUserId(), dto, lang, ct);
        return Created(result, "Food preference created successfully");
    }

    [HttpPut("pregnancies/{pregnancyId:guid}/food-preferences/{prefId:guid}")]
    [RequirePermission("food_preference.write")]
    public async Task<IActionResult> UpdatePreference(
        Guid pregnancyId, Guid prefId,
        [FromBody] UpdateFoodPreferenceDto dto,
        [FromQuery] string lang = "vi", CancellationToken ct = default)
    {
        var result = await _service.UpdatePreferenceAsync(prefId, GetCurrentUserId(), dto, lang, ct);
        return Success(result, "Food preference updated successfully");
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/food-preferences/{prefId:guid}")]
    [RequirePermission("food_preference.delete")]
    public async Task<IActionResult> DeletePreference(
        Guid pregnancyId, Guid prefId, CancellationToken ct = default)
    {
        await _service.DeletePreferenceAsync(prefId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Food preference deleted successfully");
    }

    // ═══ Nutrition Notes ═══

    [HttpGet("pregnancies/{pregnancyId:guid}/nutrition-notes")]
    [RequirePermission("nutrition_note.read")]
    public async Task<IActionResult> GetNotes(
        Guid pregnancyId, CancellationToken ct = default)
    {
        var result = await _service.GetNotesAsync(
            pregnancyId, GetCurrentUserId(), ct);
        return Success(result);
    }

    [HttpPost("pregnancies/{pregnancyId:guid}/nutrition-notes")]
    [RequirePermission("nutrition_note.write")]
    public async Task<IActionResult> CreateNote(
        Guid pregnancyId, [FromBody] CreateNutritionNoteDto dto, CancellationToken ct = default)
    {
        var result = await _service.CreateNoteAsync(
            pregnancyId, GetCurrentUserId(), dto, ct);
        return Created(result, "Nutrition note created successfully");
    }

    [HttpPut("pregnancies/{pregnancyId:guid}/nutrition-notes/{noteId:guid}")]
    [RequirePermission("nutrition_note.write")]
    public async Task<IActionResult> UpdateNote(
        Guid pregnancyId, Guid noteId,
        [FromBody] UpdateNutritionNoteDto dto, CancellationToken ct = default)
    {
        var result = await _service.UpdateNoteAsync(noteId, GetCurrentUserId(), dto, ct);
        return Success(result, "Nutrition note updated successfully");
    }

    [HttpDelete("pregnancies/{pregnancyId:guid}/nutrition-notes/{noteId:guid}")]
    [RequirePermission("nutrition_note.delete")]
    public async Task<IActionResult> DeleteNote(
        Guid pregnancyId, Guid noteId, CancellationToken ct = default)
    {
        await _service.DeleteNoteAsync(noteId, GetCurrentUserId(), ct);
        return Success<object?>(null, "Nutrition note deleted successfully");
    }
}
