using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;

namespace FPT.EXE201.Application.IServices;

public interface IWeightLogService
{
    Task<WeightLogDto> CreateAsync(Guid pregnancyId, Guid userId, CreateWeightLogDto dto, CancellationToken ct = default);
    Task<PagedResult<WeightLogDto>> GetByPregnancyIdPagedAsync(Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken ct = default);
    Task<WeightChartDataDto> GetChartDataAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightLogDto> UpdateAsync(Guid id, Guid userId, UpdateWeightLogDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default);

    // OCR Weight Extraction
    Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(Guid pregnancyId, Guid userId, Stream imageStream, string fileName, CancellationToken ct = default);

    // Weight Goals
    Task<WeightGoalDto> CreateGoalAsync(Guid pregnancyId, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default);
    Task<WeightGoalDto?> GetGoalAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightGoalDto> UpdateGoalAsync(Guid id, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default);

    // Weight Alerts
    Task<List<WeightAlertDto>> GetAlertsAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default);
    Task<WeightAlertDto> ResolveAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default);
}
