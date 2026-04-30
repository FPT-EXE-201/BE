using System.Text.Json;
using Microsoft.Extensions.Logging;
using FPT.EXE201.Application.AI;
using FPT.EXE201.Application.AI.Interfaces;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.DTOs.Nutrition;
using FPT.EXE201.Application.Exceptions;
using FPT.EXE201.Application.IServices;
using FPT.EXE201.Domain.Entities;
using FPT.EXE201.Domain.Enums;

namespace FPT.EXE201.Application.Services;

public class MealPlanService : IMealPlanService
{
    private readonly IUnitOfWork _unitOfWork;
    private readonly IAiProvider _aiProvider;
    private readonly IMealPlanJobQueue _jobQueue;
    private readonly ILogger<MealPlanService> _logger;

    private const string TemplateKey = "nutrition.meal_plan";
    private const int DailyRateLimit = 15;
    private static readonly SemaphoreSlim QueueMutationLock = new(1, 1);

    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public MealPlanService(
        IUnitOfWork unitOfWork,
        IAiProvider aiProvider,
        IMealPlanJobQueue jobQueue,
        ILogger<MealPlanService> logger)
    {
        _unitOfWork = unitOfWork;
        _aiProvider = aiProvider;
        _jobQueue = jobQueue;
        _logger = logger;
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: Queue Meal Plan Generation (returns 202 Accepted)
    // ═══════════════════════════════════════════════════

    public async Task<MealPlanStatusDto> GenerateAsync(
        Guid pregnancyId, Guid userId, GenerateMealPlanDto dto,
        CancellationToken ct = default)
    {
        // Step 1: Verify ownership
        var pregnancy = await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var planDate = dto.StartDate ?? DateOnly.FromDateTime(DateTime.UtcNow);

        // Step 2: Validate date
        if (planDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new BadRequestException("Meal plan date must be today or in the future.");

        // Step 4: Validate BMI data exists (fail-fast before queuing)
        var currentWeight = await GetCurrentWeight(pregnancyId, ct);
        var bmiWeight = pregnancy.PrePregnancyWeightKg ?? currentWeight;
        if (bmiWeight == null || pregnancy.HeightCm == null || pregnancy.HeightCm == 0)
            throw new BadRequestException(
                "Pre-pregnancy weight (or current weight) and height are required for calorie calculation.");

        // Step 5: Replace any active plan covering this date (regenerate day).
        var endDate = planDate;
        var replacementPlanIds = new List<Guid>();
        MealPlan mealPlan;

        await QueueMutationLock.WaitAsync(ct);
        try
        {
            var todayCount = await _unitOfWork.AiRequestLogs.CountTodayByUserAsync(userId, ct);
            var remaining = DailyRateLimit - todayCount;
            if (remaining < 1)
                throw new BadRequestException(
                    $"Daily AI limit reached ({DailyRateLimit}/day). Try again tomorrow.");

            var overlapping = await _unitOfWork.MealPlans
                .GetOverlappingAsync(pregnancyId, planDate, endDate, ct);
            foreach (var existing in overlapping)
            {
                if (existing.Status is MealPlanStatus.Pending or MealPlanStatus.Generating)
                    throw new BadRequestException(
                        $"Meal plan generation is already queued for {planDate:yyyy-MM-dd}. Please poll its status.");

                if (existing.Status == MealPlanStatus.Failed)
                {
                    await _unitOfWork.MealPlans.SoftDeleteAsync(existing, ct);
                    continue;
                }

                if (existing.StartDate <= planDate && existing.EndDate >= planDate)
                {
                    replacementPlanIds.Add(existing.Id);
                    _logger.LogInformation(
                        "Meal plan {PlanId} will be replaced after successful generation for {Date}",
                        existing.Id, planDate);
                }
                else
                {
                    throw new BadRequestException(
                        $"Đã có meal plan từ {existing.StartDate:yyyy-MM-dd} đến {existing.EndDate:yyyy-MM-dd}. " +
                        $"Chỉ được tạo lại từ cùng ngày bắt đầu ({existing.StartDate:yyyy-MM-dd}) " +
                        $"hoặc chọn ngày sau {existing.EndDate:yyyy-MM-dd}.");
                }
            }

            // Step 6: Create MealPlan entity with Pending status
            var aiLog = new AiRequestLog
            {
                Feature = AiFeature.NutritionMealPlan,
                PregnancyId = pregnancyId,
                UserId = userId,
                Status = AiRequestStatus.Pending
            };
            await _unitOfWork.AiRequestLogs.AddAsync(aiLog, ct);

            mealPlan = new MealPlan
            {
                PregnancyId = pregnancyId,
                AiRequestLogId = aiLog.Id,
                StartDate = planDate,
                EndDate = endDate,
                Source = MealPlanSource.AI,
                Status = MealPlanStatus.Pending,
                TotalWeeks = 1,
                CompletedWeeks = 0,
                Notes = dto.AdditionalNotes
            };
            await _unitOfWork.MealPlans.AddAsync(mealPlan, ct);
            await _unitOfWork.SaveChangesAsync(ct);
        }
        finally
        {
            QueueMutationLock.Release();
        }

        // Step 7: Enqueue for background processing
        await _jobQueue.EnqueueAsync(new MealPlanJobItem(
            mealPlan.Id, pregnancyId, userId,
            planDate, dto.AdditionalNotes, replacementPlanIds), ct);

        _logger.LogInformation(
            "Daily meal plan {PlanId} queued for generation ({Date})",
            mealPlan.Id, planDate);

        return MapToStatusDto(mealPlan);
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: Background Processing (called by BackgroundService)
    // ═══════════════════════════════════════════════════

    public async Task ProcessGenerationAsync(MealPlanJobItem job, CancellationToken ct = default)
    {
        var mealPlan = await _unitOfWork.MealPlans.GetByIdTrackedAsync(job.MealPlanId, cancellationToken: ct)
            ?? throw new NotFoundException($"MealPlan {job.MealPlanId} not found.");

        var pregnancy = await _unitOfWork.Pregnancies
            .GetByIdAsync(job.PregnancyId, cancellationToken: ct)
            ?? throw new NotFoundException("Pregnancy not found.");

        // Update status → Generating
        mealPlan.Status = MealPlanStatus.Generating;
        await _unitOfWork.SaveChangesAsync(ct);

        // Calculate BMI + target calories
        var currentWeight = await GetCurrentWeight(job.PregnancyId, ct);
        var bmiWeight = pregnancy.PrePregnancyWeightKg ?? currentWeight;
        var heightM = pregnancy.HeightCm!.Value / 100m;
        var bmi = Math.Round(bmiWeight!.Value / (heightM * heightM), 1);
        var gestWeek = pregnancy.CurrentGestationalWeek
                       ?? CalculateGestationalWeek(pregnancy.LastMenstrualPeriodDate);
        var targetCalories = CalculateTargetCalories(bmi, gestWeek ?? 20);

        // Collect nutrition context
        var foodPrefs = await _unitOfWork.FoodPreferences
            .GetByPregnancyIdAsync(job.PregnancyId, "vi", ct);
        var nutritionNotes = await _unitOfWork.NutritionNotes
            .GetByPregnancyIdAsync(job.PregnancyId, ct);
        var conditions = await _unitOfWork.PregnancyConditions
            .GetByPregnancyIdAsync(job.PregnancyId, "vi", ct);

        // Load AI template + nutrient cache
        var template = await _unitOfWork.AiPromptTemplates
            .GetActiveByKeyAsync(TemplateKey, ct)
            ?? throw new NotFoundException($"AI prompt template '{TemplateKey}' not found.");

        var allNutrients = await _unitOfWork.RefNutrients
            .GetActiveWithTranslationsAsync("vi", ct);
        var nutrientMap = allNutrients.ToDictionary(n => n.Code, n => n.Id);

        // Transaction — generate a single day.
        await _unitOfWork.BeginTransactionAsync(ct);
        int week = 0;
        try
        {
            for (week = 0; week < 1; week++)
            {
                var weekStart = job.PlanDate;
                var weekEnd = job.PlanDate;

                // Build prompt
                var contextText = FormatNutritionContext(
                    pregnancy, foodPrefs, nutritionNotes, conditions,
                    currentWeight, bmi, gestWeek, targetCalories);
                var userMessage = BuildDayPrompt(
                    weekStart, targetCalories, job.AdditionalNotes);

                var prompt = PromptBuilder.FromTemplate(template)
                    .WithContext("NUTRITION PROFILE", contextText)
                    .WithUserMessage(userMessage)
                    .Build();

                var aiLog = await GetOrCreateAiLogAsync(
                    mealPlan, job, template.Id, prompt, ct);

                _logger.LogInformation(
                    "Generating daily meal plan for {Date} and pregnancy {Id}",
                    job.PlanDate, job.PregnancyId);

                // Call Gemini
                var aiResponse = await _aiProvider.GenerateAsync(prompt, ct);

                // Parse JSON response
                var weekPlan = ParseMealPlanResponse(aiResponse.Content);

                // Set plan title from first week
                if (week == 0 && !string.IsNullOrEmpty(weekPlan.Title))
                    mealPlan.Title = weekPlan.Title;

                // Create entities from parsed response
                var newDays = CreateWeekEntities(mealPlan, weekPlan, weekStart, nutrientMap);

                // Explicitly register new entities with EF change tracker.
                // Without .Include(), the nav collection is a plain List — EF won't auto-detect additions.
                // DbSet.Add traverses the navigation graph, so child Items/Recipes/Nutrients are also tracked.
                foreach (var day in newDays)
                    await _unitOfWork.MealPlanDays.AddAsync(day, ct);

                // Update AiRequestLog → Succeeded
                aiLog.Status = AiRequestStatus.Succeeded;
                aiLog.Model = aiResponse.ModelUsed;
                aiLog.TokensInput = aiResponse.PromptTokens;
                aiLog.TokensOutput = aiResponse.CompletionTokens;
                aiLog.ProcessingTimeMs = (int)aiResponse.ProcessingTime.TotalMilliseconds;
                aiLog.ResponsePayload = aiResponse.Content;

                _logger.LogInformation(
                    "Daily meal plan generated. Tokens: {In}+{Out}={Total}",
                    aiResponse.PromptTokens,
                    aiResponse.CompletionTokens, aiResponse.TotalTokens);

                // Update progress (visible to polling)
                mealPlan.CompletedWeeks = week + 1;
                await _unitOfWork.SaveChangesAsync(ct);

            }

            // All weeks done → Succeeded
            await SoftDeleteReplacedPlansAsync(job, ct);
            mealPlan.Status = MealPlanStatus.Succeeded;
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Daily meal plan {PlanId} generated successfully for {Date}",
                mealPlan.Id, job.PlanDate);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);

            _logger.LogError(ex,
                "Meal plan generation failed for pregnancy {Id}. " +
                "Completed {CompletedWeeks}/{TotalWeeks} weeks before failure.",
                job.PregnancyId, week, 1);

            // Update MealPlan status → Failed (AFTER rollback, separate save)
            try
            {
                var failedPlan = await _unitOfWork.MealPlans
                    .GetByIdTrackedAsync(job.MealPlanId, cancellationToken: ct);
                if (failedPlan != null)
                {
                    failedPlan.Status = MealPlanStatus.Failed;
                    failedPlan.ErrorMessage = ex.Message.Length > 500
                        ? ex.Message[..500] : ex.Message;
                    failedPlan.CompletedWeeks = week;
                }

                await MarkAiLogFailedAsync(failedPlan, job, template.Id, ex, ct);
                await _unitOfWork.SaveChangesAsync(ct);
            }
            catch (Exception logEx)
            {
                _logger.LogWarning(logEx, "Could not persist failed status to DB.");
            }

            throw;
        }
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: Get Status (polling endpoint)
    // ═══════════════════════════════════════════════════

    public async Task<MealPlanStatusDto> GetStatusAsync(
        Guid planId, Guid userId, CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        return MapToStatusDto(plan);
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: List / Detail / Delete / Day Detail
    // ═══════════════════════════════════════════════════

    public async Task<PagedResult<MealPlanSummaryDto>> ListAsync(
        Guid pregnancyId, Guid userId, QueryOptions options,
        CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var paged = await _unitOfWork.MealPlans
            .GetByPregnancyIdPagedAsync(pregnancyId, options, ct);

        var dtos = paged.Items.Select(m => new MealPlanSummaryDto(
            m.Id, m.PregnancyId, m.StartDate, m.EndDate,
            m.Source.ToString(), m.Status.ToString(), m.Title,
            m.Days?.Count ?? 0, m.CreatedAt
        )).ToList();

        return new PagedResult<MealPlanSummaryDto>(
            dtos, paged.Page, paged.PageSize, paged.TotalItems);
    }

    public async Task<MealPlanDetailDto> GetDetailAsync(
        Guid planId, Guid userId, CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdWithDetailsAsync(planId, ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        return MapToDetailDto(plan);
    }

    public async Task DeleteAsync(
        Guid planId, Guid userId, CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        await _unitOfWork.MealPlans.SoftDeleteAsync(plan, ct);
        await _unitOfWork.SaveChangesAsync(ct);
    }

    public async Task<MealDayDetailDto> GetDayDetailAsync(
        Guid planId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default)
    {
        var plan = await _unitOfWork.MealPlans.GetByIdAsync(planId, cancellationToken: ct)
            ?? throw new NotFoundException("Meal plan not found.");

        await VerifyPregnancyOwnership(plan.PregnancyId, userId, ct);

        var day = await _unitOfWork.MealPlanDays
            .GetByPlanIdAndDateAsync(planId, date, ct)
            ?? throw new NotFoundException(
                $"No meal plan data for date {date:yyyy-MM-dd}.");

        return MapToDayDetailDto(day, langCode);
    }

    public async Task<MealDayDetailDto> GetDayByPregnancyDateAsync(
        Guid pregnancyId, DateOnly date, Guid userId,
        string langCode = "vi", CancellationToken ct = default)
    {
        await VerifyPregnancyOwnership(pregnancyId, userId, ct);

        var day = await _unitOfWork.MealPlanDays
            .GetByPregnancyIdAndDateAsync(pregnancyId, date, ct)
            ?? throw new NotFoundException(
                $"No meal plan data for date {date:yyyy-MM-dd}.");

        return MapToDayDetailDto(day, langCode);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Calorie Calculation (IOM Guidelines)
    // ═══════════════════════════════════════════════════

    private async Task<decimal?> GetCurrentWeight(
        Guid pregnancyId, CancellationToken ct)
    {
        var latestLog = await _unitOfWork.WeightLogs
            .GetLatestByPregnancyIdAsync(pregnancyId, ct);
        return latestLog?.WeightKg;
    }

    private static int? CalculateGestationalWeek(DateOnly? lmpDate)
    {
        if (!lmpDate.HasValue) return null;
        var today = DateOnly.FromDateTime(DateTime.UtcNow);
        var totalDays = today.DayNumber - lmpDate.Value.DayNumber;
        return totalDays >= 0 && totalDays <= 315 ? totalDays / 7 : null;
    }

    /// <summary>
    /// IOM-based calorie target: base from BMI category + trimester bonus.
    /// </summary>
    private static int CalculateTargetCalories(decimal bmi, int gestationalWeek)
    {
        var baseCalories = bmi switch
        {
            < 18.5m => 2400,       // Underweight
            < 25.0m => 2200,       // Normal
            < 30.0m => 2000,       // Overweight
            _       => 1800        // Obese
        };

        var trimesterBonus = gestationalWeek switch
        {
            <= 12 => 0,            // T1
            <= 27 => 340,          // T2
            _     => 450           // T3
        };

        return baseCalories + trimesterBonus;
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: AI Prompt Building
    // ═══════════════════════════════════════════════════

    private static string FormatNutritionContext(
        Pregnancy pregnancy,
        List<PregnancyFoodPreference> foodPrefs,
        List<PregnancyNutritionNote> nutritionNotes,
        List<PregnancyCondition> conditions,
        decimal? currentWeight, decimal bmi,
        int? gestationalWeek, int targetCalories)
    {
        var parts = new List<string>();

        if (gestationalWeek.HasValue)
            parts.Add($"Tuần thai: {gestationalWeek}");
        parts.Add($"BMI: {bmi:F1}");
        if (currentWeight.HasValue)
            parts.Add($"Cân nặng hiện tại: {currentWeight:F1} kg");
        parts.Add($"Calories mục tiêu: {targetCalories} kcal/ngày");

        var allergies = foodPrefs
            .Where(p => p.PreferenceType == FoodPreferenceType.Allergy).ToList();
        if (allergies.Any())
        {
            var names = allergies.Select(a =>
                a.FoodItem?.Translations?.FirstOrDefault()?.DisplayName
                ?? a.FoodItem?.Code ?? "N/A");
            parts.Add("Dị ứng: " + string.Join(", ", names));
        }

        var dislikes = foodPrefs
            .Where(p => p.PreferenceType == FoodPreferenceType.Dislike).ToList();
        if (dislikes.Any())
        {
            var names = dislikes.Select(d =>
                d.FoodItem?.Translations?.FirstOrDefault()?.DisplayName
                ?? d.FoodItem?.Code ?? "N/A");
            parts.Add("Không thích: " + string.Join(", ", names));
        }

        if (nutritionNotes.Any())
        {
            parts.Add("Ghi chú dinh dưỡng:");
            foreach (var note in nutritionNotes)
                parts.Add($"  - [{note.NoteType}] {note.ValueText}");
        }

        var conditionNames = conditions
            .Select(c => c.Condition?.Translations?.FirstOrDefault()?.DisplayName
                         ?? c.Condition?.Code ?? "")
            .Where(n => !string.IsNullOrEmpty(n))
            .ToList();
        if (conditionNames.Any())
            parts.Add("Bệnh lý: " + string.Join(", ", conditionNames));

        return parts.Any() ? string.Join("\n", parts) : "Không có thông tin đặc biệt.";
    }

    private static string BuildDayPrompt(
        DateOnly planDate, int targetCalories, string? additionalNotes)
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine($"Tao thuc don cho dung 1 ngay: {planDate:yyyy-MM-dd}.");
        sb.AppendLine();
        sb.AppendLine($"Muc tieu: ~{targetCalories} kcal/ngay.");
        sb.AppendLine("BAT BUOC: Tra ve dung 1 phan tu trong mang 'days' voi date trung ngay yeu cau.");
        sb.AppendLine("Ngay nay can dung 4 bua: BREAKFAST, LUNCH, DINNER, SNACK.");
        sb.AppendLine("Moi mon phai co recipe day du (title, instructions, servings, prepMinutes, cookMinutes).");
        sb.AppendLine("Giu response ngan gon, chi tra ve JSON hop le theo schema, khong them markdown.");

        if (!string.IsNullOrWhiteSpace(additionalNotes))
        {
            sb.AppendLine();
            sb.AppendLine($"Yeu cau them tu nguoi dung: {additionalNotes}");
        }

        return sb.ToString();
    }

    private static string BuildWeekPrompt(
        int weekIndex, DateOnly weekStart, DateOnly weekEnd,
        int targetCalories, string? previousWeekSummary,
        string? additionalNotes)
    {
        var sb = new System.Text.StringBuilder();

        if (weekIndex == 0)
        {
            sb.AppendLine($"Tạo thực đơn 7 ngày từ {weekStart:yyyy-MM-dd} đến {weekEnd:yyyy-MM-dd}.");
        }
        else
        {
            sb.AppendLine($"Tiếp tục thực đơn tuần {weekIndex + 1}, từ {weekStart:yyyy-MM-dd} đến {weekEnd:yyyy-MM-dd}.");
            if (!string.IsNullOrEmpty(previousWeekSummary))
            {
                sb.AppendLine();
                sb.AppendLine("Tóm tắt tuần trước:");
                sb.AppendLine(previousWeekSummary);
                sb.AppendLine("Đảm bảo đa dạng, không lặp lại món ăn tuần trước.");
            }
        }

        sb.AppendLine();
        sb.AppendLine($"Mục tiêu: ~{targetCalories} kcal/ngày.");
        sb.AppendLine("BẮT BUỘC: Trả về ĐÚng 7 ngày trong mảng 'days' (từ ngày bắt đầu đến ngày kết thúc). KHÔNG được bỏ ngày nào.");
        sb.AppendLine("Mỗi ngày cần 4 bữa: BREAKFAST, LUNCH, DINNER, SNACK.");
        sb.AppendLine("Mỗi món PHẢI có recipe đầy đủ (title, instructions, servings, prepMinutes, cookMinutes).");

        if (!string.IsNullOrEmpty(additionalNotes))
        {
            sb.AppendLine();
            sb.AppendLine($"Yêu cầu thêm từ người dùng: {additionalNotes}");
        }

        return sb.ToString();
    }

    private static string BuildWeekSummary(AiWeekResponse weekPlan)
    {
        if (weekPlan.Days == null || !weekPlan.Days.Any())
            return "Tuần trước không có dữ liệu.";

        var dishes = weekPlan.Days
            .SelectMany(d => d.Meals ?? Enumerable.Empty<AiMealResponse>())
            .Select(m => m.ItemName)
            .Where(n => !string.IsNullOrEmpty(n))
            .Distinct()
            .Take(15);

        return "Các món đã có: " + string.Join(", ", dishes);
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: JSON Parsing + Entity Creation
    // ═══════════════════════════════════════════════════

    private async Task<AiRequestLog> GetOrCreateAiLogAsync(
        MealPlan mealPlan,
        MealPlanJobItem job,
        Guid templateId,
        object prompt,
        CancellationToken ct)
    {
        AiRequestLog? aiLog = null;
        if (mealPlan.AiRequestLogId.HasValue)
        {
            aiLog = await _unitOfWork.AiRequestLogs
                .GetByIdTrackedAsync(mealPlan.AiRequestLogId.Value, cancellationToken: ct);
        }

        if (aiLog == null)
        {
            aiLog = new AiRequestLog
            {
                Feature = AiFeature.NutritionMealPlan,
                PregnancyId = job.PregnancyId,
                UserId = job.UserId
            };
            await _unitOfWork.AiRequestLogs.AddAsync(aiLog, ct);
            mealPlan.AiRequestLogId = aiLog.Id;
        }

        aiLog.TemplateId = templateId;
        aiLog.Status = AiRequestStatus.Processing;
        aiLog.RequestPayload = JsonSerializer.Serialize(prompt, JsonOptions);
        return aiLog;
    }

    private async Task SoftDeleteReplacedPlansAsync(
        MealPlanJobItem job,
        CancellationToken ct)
    {
        foreach (var planId in (job.ReplacedMealPlanIds ?? Array.Empty<Guid>()).Distinct())
        {
            if (planId == job.MealPlanId) continue;

            var replacedPlan = await _unitOfWork.MealPlans
                .GetByIdTrackedAsync(planId, cancellationToken: ct);
            if (replacedPlan == null
                || replacedPlan.PregnancyId != job.PregnancyId
                || replacedPlan.Status != MealPlanStatus.Succeeded
                || replacedPlan.StartDate > job.PlanDate
                || replacedPlan.EndDate < job.PlanDate)
            {
                continue;
            }

            await _unitOfWork.MealPlans.SoftDeleteAsync(replacedPlan, ct);
            _logger.LogInformation(
                "Replaced meal plan {PlanId} after successful generation for {Date}",
                replacedPlan.Id, job.PlanDate);
        }
    }

    private async Task MarkAiLogFailedAsync(
        MealPlan? failedPlan,
        MealPlanJobItem job,
        Guid templateId,
        Exception ex,
        CancellationToken ct)
    {
        AiRequestLog? failedLog = null;
        if (failedPlan?.AiRequestLogId != null)
        {
            failedLog = await _unitOfWork.AiRequestLogs
                .GetByIdTrackedAsync(failedPlan.AiRequestLogId.Value, cancellationToken: ct);
        }

        if (failedLog == null)
        {
            failedLog = new AiRequestLog
            {
                Feature = AiFeature.NutritionMealPlan,
                PregnancyId = job.PregnancyId,
                UserId = job.UserId
            };
            await _unitOfWork.AiRequestLogs.AddAsync(failedLog, ct);
        }

        failedLog.TemplateId = templateId;
        failedLog.Status = AiRequestStatus.Failed;
        failedLog.ErrorMessage = ex.Message.Length > 500 ? ex.Message[..500] : ex.Message;
        failedLog.ResponsePayload = JsonSerializer.Serialize(new { error = ex.Message });
    }

    private AiWeekResponse ParseMealPlanResponse(string content)
    {
        var cleaned = CleanAiJsonResponse(content);
        cleaned = RepairTruncatedJson(cleaned);

        // Retry loop: if parser finds extra bracket at a specific position,
        // remove that character and retry. Pass 1 of RepairTruncatedJson only
        // catches extra closers that make the counter go negative. Extra closers
        // inside nested structures (where counter is still >0) slip through.
        const int maxAttempts = 5;
        for (int attempt = 0; attempt < maxAttempts; attempt++)
        {
            try
            {
                var parsed = JsonSerializer.Deserialize<AiWeekResponse>(cleaned, JsonOptions);
                if (parsed?.Days == null || !parsed.Days.Any())
                    throw new BadRequestException("AI returned empty meal plan.");
                return parsed;
            }
            catch (JsonException ex) when (
                attempt < maxAttempts - 1
                && ex.Message.Contains("is invalid without a matching open")
                && ex.LineNumber.HasValue
                && ex.BytePositionInLine.HasValue)
            {
                // Remove the offending character at the exact position reported by the parser
                var lines = cleaned.Split('\n');
                var lineIdx = (int)ex.LineNumber.Value;
                var colIdx = (int)ex.BytePositionInLine.Value;

                if (lineIdx < lines.Length && colIdx < lines[lineIdx].Length)
                {
                    _logger.LogWarning(
                        "Removing extra '{Char}' at line {Line} col {Col} (attempt {Attempt})",
                        lines[lineIdx][colIdx], lineIdx, colIdx, attempt + 1);
                    lines[lineIdx] = lines[lineIdx].Remove(colIdx, 1);
                    cleaned = string.Join('\n', lines);
                    continue;
                }

                _logger.LogError(ex,
                    "Failed to parse AI meal plan response. First 300 chars: {Preview}",
                    cleaned.Length > 300 ? cleaned[..300] : cleaned);
                throw new BadRequestException(
                    "AI returned invalid meal plan format. Please try again.");
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex,
                    "Failed to parse AI meal plan response. First 300 chars: {Preview}",
                    cleaned.Length > 300 ? cleaned[..300] : cleaned);
                throw new BadRequestException(
                    "AI returned invalid meal plan format. Please try again.");
            }
        }

        throw new BadRequestException("AI returned invalid meal plan format. Please try again.");
    }

    private List<MealPlanDay> CreateWeekEntities(
        MealPlan mealPlan,
        AiWeekResponse weekPlan,
        DateOnly weekStart,
        Dictionary<string, Guid> nutrientMap)
    {
        var newDays = new List<MealPlanDay>();
        if (weekPlan.Days.Count != 1)
            throw new BadRequestException("AI must return exactly one meal plan day.");

        var dayResponse = weekPlan.Days[0];
        if (!DateOnly.TryParse(dayResponse.Date, out var responseDate) || responseDate != weekStart)
            throw new BadRequestException($"AI returned meal plan for the wrong date. Expected {weekStart:yyyy-MM-dd}.");

        if (dayResponse.Meals == null || dayResponse.Meals.Count != 4)
            throw new BadRequestException("AI must return exactly four meals for the requested day.");

        var parsedMeals = new List<(AiMealResponse Response, MealType MealType)>();
        foreach (var mealResponse in dayResponse.Meals)
        {
            if (!Enum.TryParse<MealType>(mealResponse.MealType, true, out var mealType))
                throw new BadRequestException($"AI returned an invalid meal type: {mealResponse.MealType}.");

            parsedMeals.Add((mealResponse, mealType));
        }

        var requiredMealTypes = new[] { MealType.Breakfast, MealType.Lunch, MealType.Dinner, MealType.Snack };
        if (parsedMeals.Select(m => m.MealType).Distinct().Count() != 4
            || requiredMealTypes.Any(required => parsedMeals.All(m => m.MealType != required)))
            throw new BadRequestException("AI must return one BREAKFAST, one LUNCH, one DINNER, and one SNACK.");

        var planDay = new MealPlanDay
        {
            MealPlanId = mealPlan.Id,
            PlanDate = weekStart
        };
        mealPlan.Days.Add(planDay);
        newDays.Add(planDay);

        foreach (var (mealResponse, mealType) in parsedMeals)
        {
            if (string.IsNullOrWhiteSpace(mealResponse.ItemName))
                throw new BadRequestException("AI meal item name is required.");

            if (!HasCompleteRecipe(mealResponse.Recipe))
                throw new BadRequestException($"AI meal '{mealResponse.ItemName}' must include a complete recipe.");

            var recipe = new Recipe
            {
                PregnancyId = mealPlan.PregnancyId,
                Title = mealResponse.Recipe!.Title,
                Instructions = mealResponse.Recipe.Instructions,
                Servings = mealResponse.Recipe.Servings,
                PrepMinutes = mealResponse.Recipe.PrepMinutes,
                CookMinutes = mealResponse.Recipe.CookMinutes
            };

            var mealItem = new MealItem
            {
                MealDayId = planDay.Id,
                MealType = mealType,
                RecipeId = recipe.Id,
                ItemName = mealResponse.ItemName,
                PortionText = mealResponse.PortionText,
                CaloriesKcal = mealResponse.CaloriesKcal,
                Notes = mealResponse.Notes,
                Recipe = recipe
            };
            planDay.Items.Add(mealItem);

            if (mealResponse.Nutrients == null) continue;

            foreach (var nutrientResponse in mealResponse.Nutrients)
            {
                if (string.IsNullOrWhiteSpace(nutrientResponse.Code)
                    || !nutrientMap.TryGetValue(nutrientResponse.Code, out var nutrientId))
                {
                    _logger.LogWarning(
                        "Unknown nutrient code '{Code}' - skipping",
                        nutrientResponse.Code);
                    continue;
                }

                mealItem.Nutrients.Add(new MealItemNutrient
                {
                    NutrientId = nutrientId,
                    Amount = nutrientResponse.Amount
                });
            }
        }

        return newDays;
    }

    private static bool HasCompleteRecipe(AiRecipeResponse? recipe)
    {
        return recipe != null
               && !string.IsNullOrWhiteSpace(recipe.Title)
               && !string.IsNullOrWhiteSpace(recipe.Instructions)
               && recipe.Servings.HasValue
               && recipe.PrepMinutes.HasValue
               && recipe.CookMinutes.HasValue;
    }

    private static string CleanAiJsonResponse(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return content;

        var cleaned = content.Trim();

        // Remove markdown code fences
        if (cleaned.StartsWith("```"))
        {
            var firstNewline = cleaned.IndexOf('\n');
            if (firstNewline > 0)
                cleaned = cleaned[(firstNewline + 1)..];
            if (cleaned.EndsWith("```"))
                cleaned = cleaned[..^3];
            cleaned = cleaned.Trim();
        }

        // Find JSON start
        var jsonStart = Math.Min(
            cleaned.IndexOf('{') is var ib && ib >= 0 ? ib : int.MaxValue,
            cleaned.IndexOf('[') is var ia && ia >= 0 ? ia : int.MaxValue);
        if (jsonStart > 0 && jsonStart < int.MaxValue)
            cleaned = cleaned[jsonStart..];

        // Find JSON end
        var jsonEnd = Math.Max(cleaned.LastIndexOf('}'), cleaned.LastIndexOf(']'));
        if (jsonEnd > 0 && jsonEnd < cleaned.Length - 1)
            cleaned = cleaned[..(jsonEnd + 1)];

        return cleaned;
    }

    /// <summary>
    /// Repair truncated JSON from AI response (max_output_tokens may cut off).
    /// Also removes extra closing brackets/braces mid-stream (common Gemini issue).
    /// Reuse pattern from MedicalRecordAiService.RepairTruncatedJson.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        // Pass 1: Remove extra closing brackets/braces that have no matching opener.
        // Walk char-by-char; when count goes negative, skip that character.
        var sb = new System.Text.StringBuilder(json.Length);
        var openBraces = 0;
        var openBrackets = 0;
        var inString = false;
        var escaped = false;

        foreach (var c in json)
        {
            if (escaped) { escaped = false; sb.Append(c); continue; }
            if (c == '\\' && inString) { escaped = true; sb.Append(c); continue; }
            if (c == '"') { inString = !inString; sb.Append(c); continue; }
            if (inString) { sb.Append(c); continue; }

            switch (c)
            {
                case '{': openBraces++; sb.Append(c); break;
                case '}':
                    if (openBraces > 0) { openBraces--; sb.Append(c); }
                    // else: extra closer — skip it
                    break;
                case '[': openBrackets++; sb.Append(c); break;
                case ']':
                    if (openBrackets > 0) { openBrackets--; sb.Append(c); }
                    // else: extra closer — skip it
                    break;
                default: sb.Append(c); break;
            }
        }

        if (openBraces == 0 && openBrackets == 0 && !inString)
            return sb.ToString();

        // Pass 2: handle truncation — close unclosed string, strip trailing partial entry, append closers
        var repaired = sb.ToString().TrimEnd();

        // If truncated inside a string literal, close it
        if (inString)
            repaired += "\"";
        if (repaired.Length > 0)
        {
            var lastValid = repaired.LastIndexOfAny(['}', ']', '"']);
            if (lastValid > 0)
            {
                var afterLast = repaired[(lastValid + 1)..].Trim();
                if (afterLast.Length > 0 && afterLast[0] == ',')
                    repaired = repaired[..(lastValid + 1)];
            }
        }

        for (int i = 0; i < openBrackets; i++) repaired += "]";
        for (int i = 0; i < openBraces; i++) repaired += "}";

        return repaired;
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: Ownership + Mapping
    // ═══════════════════════════════════════════════════

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

    private static MealPlanStatusDto MapToStatusDto(MealPlan plan) => new(
        plan.Id, plan.PregnancyId, plan.Status.ToString(),
        plan.CompletedWeeks, plan.TotalWeeks,
        plan.Title, plan.ErrorMessage, plan.CreatedAt);

    private static MealPlanDetailDto MapToDetailDto(MealPlan plan) => new(
        plan.Id, plan.PregnancyId, plan.StartDate, plan.EndDate,
        plan.Source.ToString(), plan.Title, plan.Notes,
        plan.Days.OrderBy(d => d.PlanDate).Select(d => new MealPlanDaySummaryDto(
            d.Id, d.PlanDate,
            d.Items.Sum(i => i.CaloriesKcal ?? 0),
            d.Items.Count
        )).ToList(),
        plan.CreatedAt, plan.UpdatedAt);

    private static MealDayDetailDto MapToDayDetailDto(MealPlanDay day, string langCode = "vi") => new(
        day.Id, day.MealPlanId, day.PlanDate,
        day.Items.Sum(i => i.CaloriesKcal ?? 0),
        day.Items.OrderBy(i => i.MealType).Select(i => new MealItemDto(
            i.Id, i.MealType.ToString(), i.RecipeId,
            i.ItemName, i.PortionText, i.CaloriesKcal, i.Notes,
            i.Nutrients.Select(n => new MealItemNutrientDto(
                n.Nutrient.Code,
                n.Nutrient.Translations.FirstOrDefault(t => t.LanguageCode == langCode)?.DisplayName
                    ?? n.Nutrient.Code,
                n.Nutrient.Unit,
                n.Amount
            )).ToList()
        )).ToList());

    // ═══════════════════════════════════════════════════
    // PRIVATE: AI Response JSON Models
    // ═══════════════════════════════════════════════════

    private record AiWeekResponse(
        string? Title,
        int? TotalDailyCalories,
        string? Notes,
        List<AiDayResponse> Days);

    private record AiDayResponse(
        string Date,
        List<AiMealResponse> Meals);

    private record AiMealResponse(
        string MealType,
        string ItemName,
        string? PortionText,
        int? CaloriesKcal,
        string? Notes,
        AiRecipeResponse? Recipe,
        List<AiNutrientResponse>? Nutrients);

    private record AiRecipeResponse(
        string Title,
        string? Instructions,
        int? Servings,
        int? PrepMinutes,
        int? CookMinutes);

    private record AiNutrientResponse(
        string Code,
        decimal Amount);
}
