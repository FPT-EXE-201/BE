using FPT.EXE201.Application.DTOs.RefData;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class RefDataService : IRefDataService
{
    private readonly IUnitOfWork _unitOfWork;

    public RefDataService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<RefConditionDto>> GetActiveConditionsAsync(string langCode, CancellationToken cancellationToken = default)
    {
        var conditions = await _unitOfWork.RefPregnancyConditions.GetActiveWithTranslationsAsync(langCode, cancellationToken);
        return conditions.Select(c =>
        {
            var t = c.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefConditionDto(c.Id, c.Code, t?.DisplayName ?? c.Code, t?.Description);
        }).ToList();
    }

    public async Task<List<RefTestTypeDto>> GetActiveTestTypesAsync(string langCode, string? category = null, CancellationToken cancellationToken = default)
    {
        var testTypes = await _unitOfWork.RefTestTypes.GetActiveWithTranslationsAsync(langCode, category, cancellationToken);
        return testTypes.Select(tt =>
        {
            var t = tt.Translations.FirstOrDefault(tr => tr.LanguageCode == langCode);
            return new RefTestTypeDto(tt.Id, tt.Code, tt.Category, t?.DisplayName ?? tt.Code, t?.Description);
        }).ToList();
    }

    public async Task<List<RefDocumentTypeDto>> GetActiveDocumentTypesAsync(string langCode, CancellationToken cancellationToken = default)
    {
        var types = await _unitOfWork.RefDocumentTypes.GetActiveWithTranslationsAsync(langCode, cancellationToken);

        return types.Select(r =>
        {
            var translation = r.Translations
                .FirstOrDefault(t => t.LanguageCode == langCode)
                ?? r.Translations.FirstOrDefault();

            return new RefDocumentTypeDto(
                r.Id, r.Code,
                translation?.DisplayName ?? r.Code,
                translation?.Description);
        }).ToList();
    }
}
