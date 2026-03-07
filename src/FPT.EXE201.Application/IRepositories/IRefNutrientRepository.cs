using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.IRepositories;

/// <summary>
/// Standalone interface — RefNutrient KHÔNG kế thừa BaseEntity (Decision #2).
/// Pattern giống IWeightAlertRepository.
/// </summary>
public interface IRefNutrientRepository
{
    Task<List<RefNutrient>> GetActiveWithTranslationsAsync(
        string langCode, CancellationToken ct = default);
    Task<List<RefNutrient>> GetByCodesAsync(
        IEnumerable<string> codes, CancellationToken ct = default);
}
