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

        // Step 2: Validate duration
        if (dto.DurationWeeks < 1 || dto.DurationWeeks > 4)
            throw new BadRequestException("Duration must be between 1 and 4 weeks.");

        // Step 3: Rate limit check
        var todayCount = await _unitOfWork.AiRequestLogs.CountTodayByUserAsync(userId, ct);
        var remaining = DailyRateLimit - todayCount;
        if (remaining < dto.DurationWeeks)
            throw new BadRequestException(
                $"Daily AI limit: need {dto.DurationWeeks} calls, remaining {remaining}/{DailyRateLimit}. Try again tomorrow.");

        // Step 4: Validate BMI data exists (fail-fast before queuing)
        var currentWeight = await GetCurrentWeight(pregnancyId, ct);
        var bmiWeight = pregnancy.PrePregnancyWeightKg ?? currentWeight;
        if (bmiWeight == null || pregnancy.HeightCm == null || pregnancy.HeightCm == 0)
            throw new BadRequestException(
                "Pre-pregnancy weight (or current weight) and height are required for calorie calculation.");

        // Step 5: Handle overlap — auto soft-delete
        var endDate = dto.StartDate.AddDays(dto.DurationWeeks * 7 - 1);
        var overlapping = await _unitOfWork.MealPlans
            .GetOverlappingAsync(pregnancyId, dto.StartDate, endDate, ct);
        foreach (var plan in overlapping)
        {
            await _unitOfWork.MealPlans.SoftDeleteAsync(plan, ct);
            _logger.LogInformation("Auto-deleted overlapping meal plan {PlanId}", plan.Id);
        }

        // Step 6: Create MealPlan entity with Pending status
        var mealPlan = new MealPlan
        {
            PregnancyId = pregnancyId,
            StartDate = dto.StartDate,
            EndDate = endDate,
            Source = MealPlanSource.AI,
            Status = MealPlanStatus.Pending,
            TotalWeeks = dto.DurationWeeks,
            CompletedWeeks = 0,
            Notes = dto.AdditionalNotes
        };
        await _unitOfWork.MealPlans.AddAsync(mealPlan, ct);
        await _unitOfWork.SaveChangesAsync(ct);

        // Step 7: Enqueue for background processing
        await _jobQueue.EnqueueAsync(new MealPlanJobItem(
            mealPlan.Id, pregnancyId, userId,
            dto.DurationWeeks, dto.AdditionalNotes), ct);

        _logger.LogInformation(
            "Meal plan {PlanId} queued for generation ({Weeks} weeks)",
            mealPlan.Id, dto.DurationWeeks);

        return MapToStatusDto(mealPlan);
    }

    // ═══════════════════════════════════════════════════
    // PUBLIC: Background Processing (called by BackgroundService)
    // ═══════════════════════════════════════════════════

    public async Task ProcessGenerationAsync(MealPlanJobItem job, CancellationToken ct = default)
    {
        var mealPlan = await _unitOfWork.MealPlans.GetByIdAsync(job.MealPlanId, cancellationToken: ct)
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

        // Transaction — Generate week by week
        await _unitOfWork.BeginTransactionAsync(ct);
        int week = 0;
        try
        {
            string? previousWeekSummary = null;

            for (week = 0; week < job.DurationWeeks; week++)
            {
                var weekStart = mealPlan.StartDate.AddDays(week * 7);
                var weekEnd = weekStart.AddDays(6);

                // Create AiRequestLog
                var aiLog = new AiRequestLog
                {
                    Feature = AiFeature.NutritionMealPlan,
                    PregnancyId = job.PregnancyId,
                    UserId = job.UserId,
                    TemplateId = template.Id,
                    Status = AiRequestStatus.Processing
                };
                await _unitOfWork.AiRequestLogs.AddAsync(aiLog, ct);

                if (week == 0) mealPlan.AiRequestLogId = aiLog.Id;

                // Build prompt
                var contextText = FormatNutritionContext(
                    pregnancy, foodPrefs, nutritionNotes, conditions,
                    currentWeight, bmi, gestWeek, targetCalories);
                var userMessage = BuildWeekPrompt(
                    week, weekStart, weekEnd, targetCalories,
                    previousWeekSummary, job.AdditionalNotes);

                var prompt = PromptBuilder.FromTemplate(template)
                    .WithContext("NUTRITION PROFILE", contextText)
                    .WithUserMessage(userMessage)
                    .Build();

                _logger.LogInformation(
                    "Generating meal plan week {Week}/{Total} for pregnancy {Id}",
                    week + 1, job.DurationWeeks, job.PregnancyId);

                // Call Gemini
                var aiResponse = await _aiProvider.GenerateAsync(prompt, ct);

                // Parse JSON response
                var weekPlan = ParseMealPlanResponse(aiResponse.Content);

                // Set plan title from first week
                if (week == 0 && !string.IsNullOrEmpty(weekPlan.Title))
                    mealPlan.Title = weekPlan.Title;

                // Create entities from parsed response
                CreateWeekEntities(mealPlan, weekPlan, weekStart, nutrientMap);

                // Update AiRequestLog → Succeeded
                aiLog.Status = AiRequestStatus.Succeeded;
                aiLog.Model = aiResponse.ModelUsed;
                aiLog.TokensInput = aiResponse.PromptTokens;
                aiLog.TokensOutput = aiResponse.CompletionTokens;
                aiLog.ProcessingTimeMs = (int)aiResponse.ProcessingTime.TotalMilliseconds;
                aiLog.ResponsePayload = aiResponse.Content;

                _logger.LogInformation(
                    "Week {Week} generated. Tokens: {In}+{Out}={Total}",
                    week + 1, aiResponse.PromptTokens,
                    aiResponse.CompletionTokens, aiResponse.TotalTokens);

                // Update progress (visible to polling)
                mealPlan.CompletedWeeks = week + 1;
                await _unitOfWork.SaveChangesAsync(ct);

                previousWeekSummary = BuildWeekSummary(weekPlan);
            }

            // All weeks done → Succeeded
            mealPlan.Status = MealPlanStatus.Succeeded;
            await _unitOfWork.CommitTransactionAsync(ct);

            _logger.LogInformation(
                "Meal plan {PlanId} generated successfully ({Weeks} weeks)",
                mealPlan.Id, job.DurationWeeks);
        }
        catch (Exception ex)
        {
            await _unitOfWork.RollbackTransactionAsync(ct);

            _logger.LogError(ex,
                "Meal plan generation failed for pregnancy {Id}. " +
                "Completed {CompletedWeeks}/{TotalWeeks} weeks before failure.",
                job.PregnancyId, week, job.DurationWeeks);

            // Update MealPlan status → Failed (AFTER rollback, separate save)
            try
            {
                var failedPlan = await _unitOfWork.MealPlans
                    .GetByIdAsync(job.MealPlanId, cancellationToken: ct);
                if (failedPlan != null)
                {
                    failedPlan.Status = MealPlanStatus.Failed;
                    failedPlan.ErrorMessage = ex.Message.Length > 500
                        ? ex.Message[..500] : ex.Message;
                    failedPlan.CompletedWeeks = week;
                }

                // Also persist a Failed AiRequestLog
                var failedLog = new AiRequestLog
                {
                    Feature = AiFeature.NutritionMealPlan,
                    PregnancyId = job.PregnancyId,
                    UserId = job.UserId,
                    TemplateId = template.Id,
                    Status = AiRequestStatus.Failed,
                    ResponsePayload = ex.Message
                };
                await _unitOfWork.AiRequestLogs.AddAsync(failedLog, ct);
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

    private AiWeekResponse ParseMealPlanResponse(string content)
    {
        var cleaned = CleanAiJsonResponse(content);
        cleaned = RepairTruncatedJson(cleaned);

        try
        {
            var parsed = JsonSerializer.Deserialize<AiWeekResponse>(cleaned, JsonOptions);
            if (parsed?.Days == null || !parsed.Days.Any())
                throw new BadRequestException("AI returned empty meal plan.");
            return parsed;
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

    private void CreateWeekEntities(
        MealPlan mealPlan,
        AiWeekResponse weekPlan,
        DateOnly weekStart,
        Dictionary<string, Guid> nutrientMap)
    {
        for (int dayIndex = 0; dayIndex < weekPlan.Days.Count; dayIndex++)
        {
            var dayResponse = weekPlan.Days[dayIndex];

            // Parse date from AI response, fallback to sequential
            DateOnly planDate;
            if (DateOnly.TryParse(dayResponse.Date, out var parsed))
                planDate = parsed;
            else
                planDate = weekStart.AddDays(dayIndex);

            var planDay = new MealPlanDay
            {
                MealPlanId = mealPlan.Id,
                PlanDate = planDate
            };
            mealPlan.Days.Add(planDay);

            if (dayResponse.Meals == null) continue;

            foreach (var mealResponse in dayResponse.Meals)
            {
                // Create Recipe (REQUIRED by business rule)
                Recipe? recipe = null;
                if (mealResponse.Recipe != null)
                {
                    recipe = new Recipe
                    {
                        PregnancyId = mealPlan.PregnancyId,
                        Title = mealResponse.Recipe.Title ?? mealResponse.ItemName ?? "Untitled",
                        Instructions = mealResponse.Recipe.Instructions,
                        Servings = mealResponse.Recipe.Servings,
                        PrepMinutes = mealResponse.Recipe.PrepMinutes,
                        CookMinutes = mealResponse.Recipe.CookMinutes
                    };
                }

                // Parse MealType
                if (!Enum.TryParse<MealType>(mealResponse.MealType, true, out var mealType))
                    mealType = MealType.Snack; // Fallback

                var mealItem = new MealItem
                {
                    MealDayId = planDay.Id,
                    MealType = mealType,
                    RecipeId = recipe?.Id,
                    ItemName = mealResponse.ItemName,
                    PortionText = mealResponse.PortionText,
                    CaloriesKcal = mealResponse.CaloriesKcal,
                    Notes = mealResponse.Notes,
                    Recipe = recipe
                };
                planDay.Items.Add(mealItem);

                // Create MealItemNutrients
                if (mealResponse.Nutrients != null)
                {
                    foreach (var nutrientResponse in mealResponse.Nutrients)
                    {
                        if (!nutrientMap.TryGetValue(nutrientResponse.Code, out var nutrientId))
                        {
                            _logger.LogWarning(
                                "Unknown nutrient code '{Code}' — skipping",
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
            }
        }
    }

    // ═══════════════════════════════════════════════════
    // PRIVATE: JSON Cleanup (reuse pattern from MedicalRecordAiService)
    // ═══════════════════════════════════════════════════

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
    /// Counts unbalanced braces/brackets and appends missing closers.
    /// Reuse pattern from MedicalRecordAiService.RepairTruncatedJson.
    /// </summary>
    private static string RepairTruncatedJson(string json)
    {
        if (string.IsNullOrWhiteSpace(json)) return json;

        var openBraces = 0;
        var openBrackets = 0;
        var inString = false;
        var escaped = false;

        foreach (var c in json)
        {
            if (escaped) { escaped = false; continue; }
            if (c == '\\') { escaped = true; continue; }
            if (c == '"') { inString = !inString; continue; }
            if (inString) continue;

            switch (c)
            {
                case '{': openBraces++; break;
                case '}': openBraces--; break;
                case '[': openBrackets++; break;
                case ']': openBrackets--; break;
            }
        }

        if (openBraces == 0 && openBrackets == 0)
            return json;

        // Strip trailing incomplete entry (partial key-value after last comma)
        var repaired = json.TrimEnd();
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

        // Append missing closers
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
