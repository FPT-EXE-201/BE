using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class FoodPreferenceService : IFoodPreferenceService
{
    private readonly IUnitOfWork _unitOfWork;

    public FoodPreferenceService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    // ═══ Food Preferences ═══

    public async Task<List<FoodPreferenceDto>> GetPreferencesAsync(
        Guid pregnancyId, Guid userId, string langCode = "vi",
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var prefs = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pregnancyId, langCode, ct);

        return prefs.Select(p => MapToPreferenceDto(p, langCode)).ToList();
    }

    public async Task<FoodPreferenceDto> CreatePreferenceAsync(
        Guid pregnancyId, Guid userId, CreateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Validate food item exists
        var foodItem = await _unitOfWork.RefFoodItems.GetByIdAsync(dto.FoodItemId, cancellationToken: ct)
            ?? throw new NotFoundException("Food item not found.");

        // Check unique constraint including soft-deleted (DB unique constraint includes them)
        var existing = await _unitOfWork.FoodPreferences
            .FindByKeyIncludingDeletedAsync(pregnancyId, dto.FoodItemId, dto.PreferenceType, ct);

        PregnancyFoodPreference pref;
        if (existing != null && existing.DeletedAt == null)
        {
            throw new ConflictException(
                $"A {dto.PreferenceType} preference for this food item already exists.");
        }
        else if (existing != null && existing.DeletedAt != null)
        {
            // Restore soft-deleted entry and update with new data
            existing.DeletedAt = null;
            existing.Severity = dto.Severity;
            existing.Notes = dto.Notes;
            existing.UpdatedAt = DateTime.UtcNow;
            _unitOfWork.FoodPreferences.Update(existing);
            pref = existing;
        }
        else
        {
            pref = new PregnancyFoodPreference
            {
                PregnancyId = pregnancyId,
                FoodItemId = dto.FoodItemId,
                PreferenceType = dto.PreferenceType,
                Severity = dto.Severity,
                Notes = dto.Notes
            };
            await _unitOfWork.FoodPreferences.AddAsync(pref, ct);
        }

        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with translations for response
        var loaded = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pregnancyId, langCode, ct);
        var created = loaded.First(p => p.Id == pref.Id);
        return MapToPreferenceDto(created, langCode);
    }

    public async Task<FoodPreferenceDto> UpdatePreferenceAsync(
        Guid prefId, Guid userId, UpdateFoodPreferenceDto dto,
        string langCode = "vi", CancellationToken ct = default)
    {
        var pref = await _unitOfWork.FoodPreferences.GetByIdAsync(prefId, cancellationToken: ct)
            ?? throw new NotFoundException("Food preference not found.");

        await VerifyPregnancyOwnership(pref.PregnancyId, userId, ct);

        if (dto.Severity != null) pref.Severity = dto.Severity;
        if (dto.Notes != null) pref.Notes = dto.Notes;

        _unitOfWork.FoodPreferences.Update(pref);
        await _unitOfWork.SaveChangesAsync(ct);

        // Reload with translations
        var loaded = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(pref.PregnancyId, langCode, ct);
        var updated = loaded.First(p => p.Id == prefId);
        return MapToPreferenceDto(updated, langCode);
    }

    public async Task DeletePreferenceAsync(
        Guid prefId, Guid userId, CancellationToken ct = default)
    {
        var pref = await _unitOfWork.FoodPreferences.GetByIdAsync(prefId, cancellationToken: ct)
            ?? throw new NotFoundException("Food preference not found.");

        await VerifyPregnancyOwnership(pref.PregnancyId, userId, ct);

        await _unitOfWork.FoodPreferences.SoftDeleteAsync(pref, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══ Nutrition Notes ═══

    public async Task<List<NutritionNoteDto>> GetNotesAsync(
        Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var notes = await _unitOfWork.NutritionNotes
            .GetByPregnancyIdAsync(pregnancyId, ct);

        return notes.Select(MapToNoteDto).ToList();
    }

    public async Task<NutritionNoteDto> CreateNoteAsync(
        Guid pregnancyId, Guid userId, CreateNutritionNoteDto dto,
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var note = new PregnancyNutritionNote
        {
            PregnancyId = pregnancyId,
            NoteType = dto.NoteType,
            ValueText = dto.ValueText
        };

        await _unitOfWork.NutritionNotes.AddAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToNoteDto(note);
    }

    public async Task<NutritionNoteDto> UpdateNoteAsync(
        Guid noteId, Guid userId, UpdateNutritionNoteDto dto,
        CancellationToken ct = default)
    {
        var note = await _unitOfWork.NutritionNotes.GetByIdAsync(noteId, cancellationToken: ct)
            ?? throw new NotFoundException("Nutrition note not found.");

        await VerifyPregnancyOwnership(note.PregnancyId, userId, ct);

        if (dto.NoteType.HasValue) note.NoteType = dto.NoteType.Value;
        if (dto.ValueText != null) note.ValueText = dto.ValueText;

        _unitOfWork.NutritionNotes.Update(note);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToNoteDto(note);
    }

    public async Task DeleteNoteAsync(
        Guid noteId, Guid userId, CancellationToken ct = default)
    {
        var note = await _unitOfWork.NutritionNotes.GetByIdAsync(noteId, cancellationToken: ct)
            ?? throw new NotFoundException("Nutrition note not found.");

        await VerifyPregnancyOwnership(note.PregnancyId, userId, ct);

        await _unitOfWork.NutritionNotes.SoftDeleteAsync(note, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══ Private Helpers ═══

    private async Task<Pregnancy> VerifyPregnancyOwnership(
        Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
        return pregnancy;
    }

    private static FoodPreferenceDto MapToPreferenceDto(
        PregnancyFoodPreference p, string langCode) => new(
        p.Id, p.PregnancyId, p.FoodItemId,
        p.FoodItem?.Code ?? "",
        p.FoodItem?.Translations?.FirstOrDefault(t => t.LanguageCode == langCode)?.DisplayName
            ?? p.FoodItem?.Code ?? "",
        p.PreferenceType.ToString(),
        p.Severity?.ToString(),
        p.Notes,
        p.CreatedAt, p.UpdatedAt);

    private static NutritionNoteDto MapToNoteDto(PregnancyNutritionNote n) => new(
        n.Id, n.PregnancyId, n.NoteType.ToString(), n.ValueText,
        n.CreatedAt, n.UpdatedAt);
}
