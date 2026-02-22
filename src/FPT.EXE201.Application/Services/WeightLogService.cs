using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.WeightTracking;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class WeightLogService : IWeightLogService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IWeightOcrService _weightOcrService;

    public WeightLogService(IUnitOfWork unitOfWork, IWeightOcrService weightOcrService)
    {
        _unitOfWork = unitOfWork;
        _weightOcrService = weightOcrService;
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT LOGS
    // ═══════════════════════════════════════════════════

    public async Task<WeightLogDto> CreateAsync(Guid pregnancyId, Guid userId, CreateWeightLogDto dto, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        // Check duplicate date
        var existing = await _unitOfWork.WeightLogs.GetByPregnancyAndDateAsync(pregnancyId, dto.LoggedOn, ct);
        if (existing != null)
            throw new ConflictException($"A weight log already exists for {dto.LoggedOn:yyyy-MM-dd}.");

        var weightLog = new WeightLog
        {
            PregnancyId = pregnancyId,
            LoggedOn = dto.LoggedOn,
            WeightKg = dto.WeightKg,
            Note = dto.Note,
            Source = dto.Source
        };

        await _unitOfWork.WeightLogs.AddAsync(weightLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Check for alerts after logging
        await CheckAndCreateAlerts(pregnancyId, weightLog, ct);

        return MapToDto(weightLog, pregnancy.PrePregnancyWeightKg);
    }

    // ═══════════════════════════════════════════════════
    // OCR WEIGHT EXTRACTION
    // ═══════════════════════════════════════════════════

    public async Task<WeightOcrExtractResultDto> ExtractWeightFromImageAsync(
        Guid pregnancyId, Guid userId, Stream imageStream, string fileName, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        return await _weightOcrService.ExtractWeightFromImageAsync(imageStream, fileName, ct);
    }

    public async Task<PagedResult<WeightLogDto>> GetByPregnancyIdPagedAsync(
        Guid pregnancyId, Guid userId, QueryOptions options, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var paged = await _unitOfWork.WeightLogs.GetByPregnancyIdPagedAsync(pregnancyId, options, ct);
        var dtos = paged.Items.Select(w => MapToDto(w, pregnancy.PrePregnancyWeightKg)).ToList();

        return new PagedResult<WeightLogDto>(dtos, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<WeightChartDataDto> GetChartDataAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var logs = await _unitOfWork.WeightLogs.GetByPregnancyIdAsync(pregnancyId, ct);
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);

        var latestLog = logs.LastOrDefault();
        var totalGain = latestLog != null && pregnancy.PrePregnancyWeightKg.HasValue
            ? latestLog.WeightKg - pregnancy.PrePregnancyWeightKg.Value
            : (decimal?)null;

        var dataPoints = logs.Select(w => new WeightChartPointDto(
            w.LoggedOn,
            w.WeightKg,
            pregnancy.LastMenstrualPeriodDate.HasValue
                ? (int)((w.LoggedOn.ToDateTime(TimeOnly.MinValue) - pregnancy.LastMenstrualPeriodDate.Value.ToDateTime(TimeOnly.MinValue)).TotalDays / 7)
                : null
        )).ToList();

        return new WeightChartDataDto(
            PrePregnancyWeightKg: pregnancy.PrePregnancyWeightKg,
            RecommendedGainMin: goal?.RecommendedTotalGainMin,
            RecommendedGainMax: goal?.RecommendedTotalGainMax,
            CurrentWeightKg: latestLog?.WeightKg,
            TotalGainKg: totalGain,
            TotalEntries: logs.Count,
            DataPoints: dataPoints
        );
    }

    public async Task<WeightLogDto> UpdateAsync(Guid id, Guid userId, UpdateWeightLogDto dto, CancellationToken ct = default)
    {
        var weightLog = await _unitOfWork.WeightLogs.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight log not found.");

        var pregnancy = await VerifyPregnancyOwnership(weightLog.PregnancyId, userId, ct);

        if (dto.WeightKg.HasValue) weightLog.WeightKg = dto.WeightKg.Value;
        if (dto.Note != null) weightLog.Note = dto.Note;
        if (dto.Source.HasValue) weightLog.Source = dto.Source.Value;

        _unitOfWork.WeightLogs.Update(weightLog);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToDto(weightLog, pregnancy.PrePregnancyWeightKg);
    }

    public async Task DeleteAsync(Guid id, Guid userId, CancellationToken ct = default)
    {
        var weightLog = await _unitOfWork.WeightLogs.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight log not found.");

        await VerifyPregnancyOwnership(weightLog.PregnancyId, userId, ct);

        await _unitOfWork.WeightLogs.SoftDeleteAsync(weightLog, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT GOALS
    // ═══════════════════════════════════════════════════

    public async Task<WeightGoalDto> CreateGoalAsync(Guid pregnancyId, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default)
    {
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var existing = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        if (existing != null)
            throw new ConflictException("Weight goal already exists for this pregnancy. Use PUT to update.");

        var heightCm = dto.HeightCm ?? pregnancy.HeightCm;
        var preWeight = dto.PrePregnancyWeightKg ?? pregnancy.PrePregnancyWeightKg;
        var bmi = CalculateBmi(preWeight, heightCm);

        // Auto IOM guidelines if user did not provide custom range
        var (gainMin, gainMax) = dto.RecommendedTotalGainMin.HasValue && dto.RecommendedTotalGainMax.HasValue
            ? (dto.RecommendedTotalGainMin.Value, dto.RecommendedTotalGainMax.Value)
            : GetIomRecommendation(bmi);

        var goal = new WeightGoalRange
        {
            PregnancyId = pregnancyId,
            HeightCm = heightCm,
            PrePregnancyWeightKg = preWeight,
            Bmi = bmi,
            RecommendedTotalGainMin = gainMin,
            RecommendedTotalGainMax = gainMax,
            Notes = dto.Notes
        };

        await _unitOfWork.WeightGoalRanges.AddAsync(goal, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToGoalDto(goal);
    }

    public async Task<WeightGoalDto?> GetGoalAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        return goal == null ? null : MapToGoalDto(goal);
    }

    public async Task<WeightGoalDto> UpdateGoalAsync(Guid id, Guid userId, CreateWeightGoalDto dto, CancellationToken ct = default)
    {
        var goal = await _unitOfWork.WeightGoalRanges.GetByIdAsync(id, cancellationToken: ct)
            ?? throw new NotFoundException("Weight goal not found.");

        await VerifyPregnancyOwnership(goal.PregnancyId, userId, ct);

        if (dto.HeightCm.HasValue) goal.HeightCm = dto.HeightCm;
        if (dto.PrePregnancyWeightKg.HasValue) goal.PrePregnancyWeightKg = dto.PrePregnancyWeightKg;
        goal.Bmi = CalculateBmi(goal.PrePregnancyWeightKg, goal.HeightCm);

        if (dto.RecommendedTotalGainMin.HasValue) goal.RecommendedTotalGainMin = dto.RecommendedTotalGainMin;
        if (dto.RecommendedTotalGainMax.HasValue) goal.RecommendedTotalGainMax = dto.RecommendedTotalGainMax;
        if (dto.Notes != null) goal.Notes = dto.Notes;

        _unitOfWork.WeightGoalRanges.Update(goal);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToGoalDto(goal);
    }

    // ═══════════════════════════════════════════════════
    // WEIGHT ALERTS
    // ═══════════════════════════════════════════════════

    public async Task<List<WeightAlertDto>> GetAlertsAsync(Guid pregnancyId, Guid userId, CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);
        var alerts = await _unitOfWork.WeightAlerts.GetByPregnancyIdAsync(pregnancyId, ct);
        return alerts.Select(MapToAlertDto).ToList();
    }

    public async Task<WeightAlertDto> ResolveAlertAsync(Guid alertId, Guid userId, CancellationToken ct = default)
    {
        var alert = await _unitOfWork.WeightAlerts.GetByIdAsync(alertId, ct)
            ?? throw new NotFoundException("Weight alert not found.");

        await VerifyPregnancyOwnership(alert.PregnancyId, userId, ct);

        if (alert.ResolvedAt.HasValue)
            throw new BadRequestException("Alert is already resolved.");

        alert.ResolvedAt = DateTime.UtcNow;
        _unitOfWork.WeightAlerts.Update(alert);
        await _unitOfWork.SaveChangesAsync(ct);

        return MapToAlertDto(alert);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE HELPERS
    // ═══════════════════════════════════════════════════

    private async Task<Pregnancy> VerifyPregnancyOwnership(Guid pregnancyId, Guid userId, CancellationToken ct)
    {
        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");
        if (pregnancy.UserId != userId)
            throw new ForbiddenException("Access denied.");
        return pregnancy;
    }

    private async Task CheckAndCreateAlerts(Guid pregnancyId, WeightLog newLog, CancellationToken ct)
    {
        var goal = await _unitOfWork.WeightGoalRanges.GetByPregnancyIdAsync(pregnancyId, ct);
        if (goal == null) return;

        var pregnancy = await _unitOfWork.Pregnancies.GetByIdAsync(pregnancyId, cancellationToken: ct);
        if (pregnancy == null) return;

        // Check total gain vs recommended range
        if (goal.PrePregnancyWeightKg.HasValue)
        {
            var totalGain = newLog.WeightKg - goal.PrePregnancyWeightKg.Value;

            if (goal.RecommendedTotalGainMax.HasValue && totalGain > goal.RecommendedTotalGainMax.Value)
            {
                await CreateAlert(pregnancyId, WeightAlertType.AboveRange,
                    $"{{\"currentWeight\":{newLog.WeightKg},\"totalGain\":{totalGain},\"maxRecommended\":{goal.RecommendedTotalGainMax}}}", ct);
            }
            else if (goal.RecommendedTotalGainMin.HasValue && totalGain < goal.RecommendedTotalGainMin.Value
                     && pregnancy.CurrentGestationalWeek >= 37)
            {
                await CreateAlert(pregnancyId, WeightAlertType.BelowRange,
                    $"{{\"currentWeight\":{newLog.WeightKg},\"totalGain\":{totalGain},\"minRecommended\":{goal.RecommendedTotalGainMin}}}", ct);
            }
        }

        // Check rapid gain/loss — compare with log from ~1 week ago (7–14 day window)
        // Fetch recent logs, skip the current one (index 0), find the first one >= 7 days apart
        var recentLogs = await _unitOfWork.WeightLogs.GetRecentByPregnancyIdAsync(pregnancyId, 15, ct);
        var compareLog = recentLogs
            .Skip(1) // skip current log (newest)
            .FirstOrDefault(log =>
            {
                var diff = (newLog.LoggedOn.ToDateTime(TimeOnly.MinValue) - log.LoggedOn.ToDateTime(TimeOnly.MinValue)).TotalDays;
                return diff >= 7 && diff <= 14;
            });

        if (compareLog != null)
        {
            var daysDiff = (newLog.LoggedOn.ToDateTime(TimeOnly.MinValue) - compareLog.LoggedOn.ToDateTime(TimeOnly.MinValue)).TotalDays;
            var weeklyGain = (newLog.WeightKg - compareLog.WeightKg) / (decimal)(daysDiff / 7.0);
            if (weeklyGain > 0.7m)
            {
                // Cooldown: only create alert if no RapidGain alert in the last 7 days
                if (!await _unitOfWork.WeightAlerts.HasRecentAlertAsync(pregnancyId, WeightAlertType.RapidGain, 7, ct))
                {
                    await CreateAlert(pregnancyId, WeightAlertType.RapidGain,
                        $"{{\"weeklyGain\":{weeklyGain:F2},\"currentWeight\":{newLog.WeightKg},\"previousWeight\":{compareLog.WeightKg},\"daysBetween\":{daysDiff}}}", ct);
                }
            }
            else if (weeklyGain < -0.3m)
            {
                if (!await _unitOfWork.WeightAlerts.HasRecentAlertAsync(pregnancyId, WeightAlertType.RapidLoss, 7, ct))
                {
                    await CreateAlert(pregnancyId, WeightAlertType.RapidLoss,
                        $"{{\"weeklyChange\":{weeklyGain:F2},\"currentWeight\":{newLog.WeightKg},\"previousWeight\":{compareLog.WeightKg},\"daysBetween\":{daysDiff}}}", ct);
                }
            }
        }
    }

    private async Task CreateAlert(Guid pregnancyId, WeightAlertType type, string detailsJson, CancellationToken ct)
    {
        var alert = new WeightAlert
        {
            PregnancyId = pregnancyId,
            AlertType = type,
            TriggeredAt = DateTime.UtcNow,
            DetailsJson = detailsJson
        };
        await _unitOfWork.WeightAlerts.AddAsync(alert, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    private static decimal? CalculateBmi(decimal? weightKg, decimal? heightCm)
    {
        if (!weightKg.HasValue || !heightCm.HasValue || heightCm.Value == 0) return null;
        var heightM = heightCm.Value / 100m;
        return Math.Round(weightKg.Value / (heightM * heightM), 2);
    }

    private static (decimal min, decimal max) GetIomRecommendation(decimal? bmi)
    {
        if (!bmi.HasValue) return (11.5m, 16.0m);
        return bmi.Value switch
        {
            < 18.5m => (12.5m, 18.0m),
            < 25.0m => (11.5m, 16.0m),
            < 30.0m => (7.0m, 11.5m),
            _       => (5.0m, 9.0m)
        };
    }

    private static string GetBmiCategory(decimal? bmi)
    {
        if (!bmi.HasValue) return "Unknown";
        return bmi.Value switch
        {
            < 18.5m => "Underweight",
            < 25.0m => "Normal",
            < 30.0m => "Overweight",
            _       => "Obese"
        };
    }

    private static WeightLogDto MapToDto(WeightLog w, decimal? prePregnancyWeight) => new(
        w.Id, w.PregnancyId, w.LoggedOn, w.WeightKg, w.Note, w.Source.ToString(),
        prePregnancyWeight.HasValue ? w.WeightKg - prePregnancyWeight.Value : null,
        w.CreatedAt, w.UpdatedAt);

    private static WeightGoalDto MapToGoalDto(WeightGoalRange g) => new(
        g.Id, g.PregnancyId, g.HeightCm, g.PrePregnancyWeightKg, g.Bmi,
        GetBmiCategory(g.Bmi),
        g.RecommendedTotalGainMin, g.RecommendedTotalGainMax,
        g.Notes, g.CreatedAt, g.UpdatedAt);

    private static WeightAlertDto MapToAlertDto(WeightAlert a) => new(
        a.Id, a.PregnancyId, a.AlertType.ToString(), a.TriggeredAt,
        a.DetailsJson, a.ResolvedAt, a.ResolvedAt.HasValue);
}
