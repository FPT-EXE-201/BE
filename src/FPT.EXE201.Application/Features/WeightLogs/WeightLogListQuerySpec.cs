using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.WeightLogs;

/// <summary>
/// Query specification for WeightLog entity listing.
/// Searchable: note | Sortable: loggedon, weightkg, createdat
/// </summary>
public static class WeightLogListQuerySpec
{
    // ─── Search whitelist ──────────────────────────────────────
    public static readonly Dictionary<string, Expression<Func<WeightLog, string?>>> SearchMap = new()
    {
        ["note"] = w => w.Note
    };

    public static readonly string[] DefaultSearchKeys = ["note"];

    // ─── Sort whitelist ────────────────────────────────────────
    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["loggedon"]  = (Expression<Func<WeightLog, DateOnly>>)(w => w.LoggedOn),
        ["weightkg"]  = (Expression<Func<WeightLog, decimal>>)(w => w.WeightKg),
        ["createdat"] = (Expression<Func<WeightLog, DateTime>>)(w => w.CreatedAt)
    };

    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<WeightLog, DateOnly>>)(w => w.LoggedOn);

    // ─── Metadata cho FE ───────────────────────────────────────
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "loggedon",
        DefaultSortDir = "desc"
    };
}
