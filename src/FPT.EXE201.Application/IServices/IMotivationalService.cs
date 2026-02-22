using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

public interface IMotivationalService
{
    Task<List<MotivationalTemplateDto>> GetByWeekAsync(int week, string? category = null, string langCode = "vi", CancellationToken ct = default);
}
