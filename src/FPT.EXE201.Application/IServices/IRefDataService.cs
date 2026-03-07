using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.DTOs.RefData;

namespace FPT.EXE201.Application.IServices;

/// <summary>
/// Service cho reference/lookup data — public endpoints, không cần auth.
/// User cần lấy danh mục bệnh lý và xét nghiệm trước khi tạo records.
/// </summary>
public interface IRefDataService
{
    Task<List<RefConditionDto>> GetActiveConditionsAsync(string langCode, CancellationToken cancellationToken = default);
    Task<List<RefTestTypeDto>> GetActiveTestTypesAsync(string langCode, string? category = null, CancellationToken cancellationToken = default);
    Task<List<RefDocumentTypeDto>> GetActiveDocumentTypesAsync(string langCode, CancellationToken cancellationToken = default);

    // Week 7 — Nutrition
    Task<List<RefFoodItemDto>> GetActiveFoodItemsAsync(
        string langCode, CancellationToken cancellationToken = default);
    Task<List<RefNutrientDto>> GetActiveNutrientsAsync(
        string langCode, CancellationToken cancellationToken = default);
}
