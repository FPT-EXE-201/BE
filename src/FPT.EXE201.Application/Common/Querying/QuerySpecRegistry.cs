using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Application.Features.Languages;
using FPT.EXE201.Application.Features.MealPlans;
using FPT.EXE201.Application.Features.PrenatalTests;
using FPT.EXE201.Application.Features.PrenatalVisits;
using FPT.EXE201.Application.Features.WeightLogs;

namespace FPT.EXE201.Application.Common.Querying;

/// <summary>
/// Central registry of all QuerySpec metadata.
/// FE gọi GET /api/ref/query-specs → nhận được tất cả search/sort capabilities.
/// Khi thêm feature mới (weight log, nutrition, chat,...), chỉ cần thêm 1 dòng ở đây.
/// </summary>
public static class QuerySpecRegistry
{
    /// <summary>
    /// Key = resource name (camelCase, khớp với endpoint).
    /// Value = metadata describing search/sort capabilities.
    /// </summary>
    public static readonly IReadOnlyDictionary<string, QuerySpecMetadataDto> All =
        new Dictionary<string, QuerySpecMetadataDto>
        {
            ["prenatalVisits"] = PrenatalVisitListQuerySpec.Metadata,
            ["prenatalTests"] = PrenatalTestListQuerySpec.Metadata,
            ["languages"] = LanguageListQuerySpec.Metadata,
            ["weightLogs"] = WeightLogListQuerySpec.Metadata,
            ["mealPlans"] = MealPlanListQuerySpec.Metadata,
        };
}
