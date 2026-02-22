using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.IServices;

namespace FPT.EXE201.Application.Services;

public class MotivationalService : IMotivationalService
{
    private readonly IUnitOfWork _unitOfWork;

    public MotivationalService(IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
    }

    public async Task<List<MotivationalTemplateDto>> GetByWeekAsync(
        int week, string? category = null, string langCode = "vi", CancellationToken ct = default)
    {
        var templates = await _unitOfWork.MotivationalTemplates
            .GetByWeekAsync(week, category, langCode, ct);

        return templates.Select(t =>
        {
            var translation = t.Translations.FirstOrDefault();
            return new MotivationalTemplateDto(
                t.Id,
                t.Category.ToString(),
                t.WeekStart,
                t.WeekEnd,
                t.VariablesJson,
                translation?.Title,
                translation?.Message ?? string.Empty
            );
        }).ToList();
    }
}
