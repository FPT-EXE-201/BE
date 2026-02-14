using System.Linq.Expressions;
using FPT.EXE201.Application.DTOs.Common;
using FPT.EXE201.Domain.Entities;

namespace FPT.EXE201.Application.Features.PrenatalTests;

/// <summary>
/// Query specification for PrenatalTest entity listing.
/// Searchable: notes | Sortable: testDate, createdAt
/// </summary>
public static class PrenatalTestListQuerySpec
{
    public static readonly Dictionary<string, Expression<Func<PrenatalTest, string?>>> SearchMap = new()
    {
        ["notes"] = t => t.Notes
    };

    public static readonly string[] DefaultSearchKeys = ["notes"];

    public static readonly Dictionary<string, LambdaExpression> SortMap = new()
    {
        ["testdate"] = (Expression<Func<PrenatalTest, DateOnly>>)(t => t.TestDate),
        ["createdat"] = (Expression<Func<PrenatalTest, DateTime>>)(t => t.CreatedAt)
    };

    public static readonly LambdaExpression DefaultSort =
        (Expression<Func<PrenatalTest, DateOnly>>)(t => t.TestDate);

    /// <summary>Metadata for FE: GET /api/ref/query-specs</summary>
    public static readonly QuerySpecMetadataDto Metadata = new()
    {
        SearchableFields = SearchMap.Keys.ToList(),
        DefaultSearchFields = DefaultSearchKeys,
        SortableFields = SortMap.Keys.ToList(),
        DefaultSortBy = "testdate",
        DefaultSortDir = "desc"
    };
}
